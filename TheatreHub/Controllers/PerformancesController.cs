using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheatreHub.Data;
using TheatreHub.Models;

namespace TheatreHub.Controllers;

public class PerformancesController : Controller
{
    private readonly ApplicationDbContext _context;

    public PerformancesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Performances
    public async Task<IActionResult> Index()
    {
        var performances = await _context.Performances
            .AsNoTracking()
            .OrderBy(performance => performance.Title)
            .ToListAsync();

        return View(performances);
    }

    // GET: Performances/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var performance = await _context.Performances
            .AsNoTracking()
            .Include(item => item.Acts)
                .ThenInclude(act => act.Scenes)
            .FirstOrDefaultAsync(item => item.Id == id.Value);

        if (performance == null)
        {
            return NotFound();
        }

        SortStructure(performance);

        return View(performance);
    }

    // GET: Performances/Create
    public IActionResult Create()
    {
        return View(new Performance());
    }

    // POST: Performances/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(
            "Title,Description,Genre,PremiereDate," +
            "DurationMinutes,Status")]
        Performance performance,
        string? submitAction)
    {
        NormalizePerformance(performance);

        if (!ModelState.IsValid)
        {
            return View(performance);
        }

        if (performance.CreatedAt == default)
        {
            performance.CreatedAt = DateTime.Now;
        }

        _context.Performances.Add(performance);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"Виставу «{performance.Title}» успішно створено.";

        if (submitAction == "structure")
        {
            return RedirectToAction(
                "Index",
                "Acts",
                new
                {
                    performanceId = performance.Id
                });
        }

        return RedirectToAction(
            nameof(Details),
            new
            {
                id = performance.Id
            });
    }

    // GET: Performances/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var performance =
            await GetPerformanceWithStructureAsync(id.Value);

        if (performance == null)
        {
            return NotFound();
        }

        return View(performance);
    }

    // POST: Performances/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind(
            "Id,Title,Description,Genre,PremiereDate," +
            "DurationMinutes,Status")]
        Performance performance,
        string? submitAction)
    {
        if (id != performance.Id)
        {
            return NotFound();
        }

        NormalizePerformance(performance);

        if (!ModelState.IsValid)
        {
            await LoadStructureAsync(performance);

            return View(performance);
        }

        var existingPerformance =
            await _context.Performances
                .FirstOrDefaultAsync(item =>
                    item.Id == id);

        if (existingPerformance == null)
        {
            return NotFound();
        }

        existingPerformance.Title =
            performance.Title;

        existingPerformance.Description =
            performance.Description;

        existingPerformance.Genre =
            performance.Genre;

        existingPerformance.PremiereDate =
            performance.PremiereDate;

        existingPerformance.DurationMinutes =
            performance.DurationMinutes;

        existingPerformance.Status =
            performance.Status;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await PerformanceExistsAsync(id))
            {
                return NotFound();
            }

            throw;
        }

        TempData["SuccessMessage"] =
            $"Виставу «{existingPerformance.Title}» успішно оновлено.";

        if (submitAction == "structure")
        {
            return RedirectToAction(
                "Index",
                "Acts",
                new
                {
                    performanceId = id
                });
        }

        return RedirectToAction(
            nameof(Edit),
            new
            {
                id
            });
    }

    // GET: Performances/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var performance = await _context.Performances
            .AsNoTracking()
            .Include(item => item.Acts)
                .ThenInclude(act => act.Scenes)
            .FirstOrDefaultAsync(item =>
                item.Id == id.Value);

        if (performance == null)
        {
            return NotFound();
        }

        return View(performance);
    }

    // POST: Performances/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(
        int id)
    {
        var performance = await _context.Performances
            .FirstOrDefaultAsync(item =>
                item.Id == id);

        if (performance == null)
        {
            return NotFound();
        }

        var performanceTitle = performance.Title;

        _context.Performances.Remove(performance);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"Виставу «{performanceTitle}» видалено.";

        return RedirectToAction(nameof(Index));
    }

    private async Task<Performance?>
        GetPerformanceWithStructureAsync(int id)
    {
        var performance = await _context.Performances
            .AsNoTracking()
            .Include(item => item.Acts)
                .ThenInclude(act => act.Scenes)
            .FirstOrDefaultAsync(item =>
                item.Id == id);

        if (performance != null)
        {
            SortStructure(performance);
        }

        return performance;
    }

    private async Task LoadStructureAsync(
        Performance performance)
    {
        var acts = await _context.Acts
            .AsNoTracking()
            .Include(act => act.Scenes)
            .Where(act =>
                act.PerformanceId == performance.Id)
            .OrderBy(act => act.Position)
            .ThenBy(act => act.Number)
            .ToListAsync();

        foreach (var act in acts)
        {
            act.Scenes = act.Scenes
                .OrderBy(scene => scene.Position)
                .ThenBy(scene => scene.Number)
                .ToList();
        }

        performance.Acts = acts;
    }

    private static void SortStructure(
        Performance performance)
    {
        performance.Acts = performance.Acts
            .OrderBy(act => act.Position)
            .ThenBy(act => act.Number)
            .ToList();

        foreach (var act in performance.Acts)
        {
            act.Scenes = act.Scenes
                .OrderBy(scene => scene.Position)
                .ThenBy(scene => scene.Number)
                .ToList();
        }
    }

    private static void NormalizePerformance(
        Performance performance)
    {
        performance.Title =
            performance.Title?.Trim()
            ?? string.Empty;

        performance.Description =
            NormalizeOptionalText(
                performance.Description);

        performance.Genre =
            performance.Genre?.Trim()
            ?? string.Empty;
    }

    private static string? NormalizeOptionalText(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private async Task<bool> PerformanceExistsAsync(
        int id)
    {
        return await _context.Performances
            .AnyAsync(item => item.Id == id);
    }
}