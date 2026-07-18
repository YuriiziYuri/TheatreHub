using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheatreHub.Constants;
using TheatreHub.Data;
using TheatreHub.ViewModels.ActionLogs;
using ClosedXML.Excel;
using TheatreHub.Models;

namespace TheatreHub.Controllers;

[Authorize(Roles = $"{UserRoles.SystemAdmin},{UserRoles.GeneralDirector}")]
public class AdminActionLogsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminActionLogsController(ApplicationDbContext context)
    {
        _context = context;
    }
    private IQueryable<UserActionLog> ApplyFilters(
    IQueryable<UserActionLog> query,
    string? search,
    string? actionType,
    string? entityType,
    DateTime? dateFrom,
    DateTime? dateTo)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch =
                search.Trim().ToLower();

            query = query.Where(log =>
                log.UserFullName.ToLower().Contains(normalizedSearch)
                || log.Description.ToLower().Contains(normalizedSearch)
                || (log.EntityTitle != null &&
                    log.EntityTitle.ToLower().Contains(normalizedSearch)));
        }

        if (!string.IsNullOrWhiteSpace(actionType))
        {
            query = query.Where(log =>
                log.ActionType == actionType);
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(log =>
                log.EntityType == entityType);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(log =>
                log.CreatedAt.Date >= dateFrom.Value.Date);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(log =>
                log.CreatedAt.Date <= dateTo.Value.Date);
        }

        return query;
    }

    public async Task<IActionResult> Index(
    string? search,
    string? actionType,
    string? entityType,
    DateTime? dateFrom,
    DateTime? dateTo)
    {
        var query =
            _context.UserActionLogs
                .AsNoTracking()
                .AsQueryable();

        query = ApplyFilters(
            query,
            search,
            actionType,
            entityType,
            dateFrom,
            dateTo);

        var model = new UserActionLogIndexViewModel
        {
            Logs = await query
                .OrderByDescending(log => log.CreatedAt)
                .Take(300)
                .ToListAsync(),

            Search = search,
            ActionType = actionType,
            EntityType = entityType,
            DateFrom = dateFrom,
            DateTo = dateTo,

            ActionTypes = await _context.UserActionLogs
                .AsNoTracking()
                .Select(log => log.ActionType)
                .Distinct()
                .OrderBy(value => value)
                .ToListAsync(),

            EntityTypes = await _context.UserActionLogs
                .AsNoTracking()
                .Select(log => log.EntityType)
                .Distinct()
                .OrderBy(value => value)
                .ToListAsync()
        };

        return View(model);
    }

    public async Task<IActionResult> ExportToExcel(
    string? search,
    string? actionType,
    string? entityType,
    DateTime? dateFrom,
    DateTime? dateTo)
    {
        var query =
            _context.UserActionLogs
                .AsNoTracking()
                .AsQueryable();

        query = ApplyFilters(
            query,
            search,
            actionType,
            entityType,
            dateFrom,
            dateTo);

        var logs = await query
            .OrderByDescending(log => log.CreatedAt)
            .ToListAsync();

        using var workbook = new XLWorkbook();

        var worksheet =
            workbook.Worksheets.Add("Журнал дій");

        worksheet.Cell(1, 1).Value = "Дата і час";
        worksheet.Cell(1, 2).Value = "Користувач";
        worksheet.Cell(1, 3).Value = "Дія";
        worksheet.Cell(1, 4).Value = "Тип об'єкта";
        worksheet.Cell(1, 5).Value = "Назва об'єкта";
        worksheet.Cell(1, 6).Value = "Опис";

        var headerRange =
            worksheet.Range(1, 1, 1, 6);

        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor =
            XLColor.LightGray;

        for (var i = 0; i < logs.Count; i++)
        {
            var log = logs[i];
            var row = i + 2;

            worksheet.Cell(row, 1).Value =
                log.CreatedAt.ToString("dd.MM.yyyy HH:mm:ss");

            worksheet.Cell(row, 2).Value =
                log.UserFullName;

            worksheet.Cell(row, 3).Value =
                log.ActionType;

            worksheet.Cell(row, 4).Value =
                log.EntityType;

            worksheet.Cell(row, 5).Value =
                log.EntityTitle ?? "";

            worksheet.Cell(row, 6).Value =
                log.Description;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();

        workbook.SaveAs(stream);

        var fileName =
            $"user-action-logs-{DateTime.Now:yyyyMMdd-HHmm}.xlsx";

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = UserRoles.SystemAdmin)]
    public async Task<IActionResult> Clear()
    {
        var deletedCount =
            await _context.UserActionLogs
                .ExecuteDeleteAsync();

        TempData["SuccessMessage"] =
            deletedCount > 0
                ? $"Журнал дій очищено. Видалено записів: {deletedCount}."
                : "Журнал дій уже порожній.";

        return RedirectToAction(nameof(Index));
    }
}