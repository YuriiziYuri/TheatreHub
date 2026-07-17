using System.IO.Compression;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using TheatreHub.Constants;
using TheatreHub.Services.ActionLogs;

namespace TheatreHub.Controllers;

[Authorize(Roles = $"{UserRoles.SystemAdmin},{UserRoles.GeneralDirector}")]
public class AdminBackupsController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly IUserActionLogService _actionLogService;

    public AdminBackupsController(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        IUserActionLogService actionLogService)
    {
        _configuration = configuration;
        _environment = environment;
        _actionLogService = actionLogService;
    }

    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> DownloadDatabaseBackup()
    {
        var connectionString =
            _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            TempData["ErrorMessage"] =
                "Не знайдено рядок підключення до бази даних.";

            return RedirectToAction(nameof(Index));
        }

        var databasePath =
            GetSqliteDatabasePath(connectionString);

        if (string.IsNullOrWhiteSpace(databasePath))
        {
            TempData["ErrorMessage"] =
                "Не вдалося визначити шлях до SQLite-бази.";

            return RedirectToAction(nameof(Index));
        }

        var fullDatabasePath =
            Path.IsPathRooted(databasePath)
                ? databasePath
                : Path.Combine(
                    _environment.ContentRootPath,
                    databasePath);

        if (!System.IO.File.Exists(fullDatabasePath))
        {
            TempData["ErrorMessage"] =
                "Файл бази даних не знайдено.";

            return RedirectToAction(nameof(Index));
        }

        var backupFileName =
            $"theatrehub-backup-{DateTime.Now:yyyyMMdd-HHmmss}.zip";

        await using var memoryStream =
            new MemoryStream();

        using (var archive = new ZipArchive(
            memoryStream,
            ZipArchiveMode.Create,
            leaveOpen: true))
        {
            var databaseEntry =
                archive.CreateEntry(
                    Path.GetFileName(fullDatabasePath),
                    CompressionLevel.Fastest);

            await using var entryStream =
                databaseEntry.Open();

            await using var databaseStream =
                System.IO.File.Open(
                    fullDatabasePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite);

            await databaseStream.CopyToAsync(entryStream);
        }

        await _actionLogService.LogAsync(
            User,
            "Create",
            "Backup",
            null,
            backupFileName,
            $"Створено резервну копію бази даних «{backupFileName}».");

        return File(
            memoryStream.ToArray(),
            "application/zip",
            backupFileName);
    }

    private static string? GetSqliteDatabasePath(
        string connectionString)
    {
        var parts =
            connectionString.Split(
                ';',
                StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            var keyValue =
                part.Split(
                    '=',
                    2,
                    StringSplitOptions.TrimEntries);

            if (keyValue.Length == 2 &&
                keyValue[0].Equals(
                    "Data Source",
                    StringComparison.OrdinalIgnoreCase))
            {
                return keyValue[1];
            }
        }

        return null;
    }
}