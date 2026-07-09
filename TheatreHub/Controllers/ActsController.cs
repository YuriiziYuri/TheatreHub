using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheatreHub.Data;
using TheatreHub.Models;

namespace TheatreHub.Controllers;

public class ActsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ActsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Acts?performanceId=5
    public async Task<IActionResult> Index(int performanceId)
    {
        var performance = await _context.Performances
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.Id == performanceId);

        if (performance == null)
        {
            return NotFound();
        }

        var acts = await _context.Acts
            .AsNoTracking()
            .Include(act => act.Scenes)
            .Where(act =>
                act.PerformanceId == performanceId)
            .OrderBy(act => act.Position)
            .ThenBy(act => act.Number)
            .ToListAsync();

        ViewBag.PerformanceId = performance.Id;
        ViewBag.PerformanceTitle = performance.Title;

        return View(acts);
    }

    // GET: Acts/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var act = await _context.Acts
            .AsNoTracking()
            .Include(item => item.Performance)
            .Include(item => item.Scenes)
            .FirstOrDefaultAsync(item =>
                item.Id == id.Value);

        if (act == null)
        {
            return NotFound();
        }

        act.Scenes = act.Scenes
            .OrderBy(scene => scene.Position)
            .ThenBy(scene => scene.Number)
            .ToList();

        return View(act);
    }

    // GET: Acts/Create?performanceId=5
    public async Task<IActionResult> Create(
        int performanceId)
    {
        var performance = await _context.Performances
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.Id == performanceId);

        if (performance == null)
        {
            return NotFound();
        }

        var nextNumber = await _context.Acts
            .Where(act =>
                act.PerformanceId == performanceId)
            .Select(act => (int?)act.Number)
            .MaxAsync() ?? 0;

        var nextPosition = await _context.Acts
            .Where(act =>
                act.PerformanceId == performanceId)
            .Select(act => (int?)act.Position)
            .MaxAsync() ?? 0;

        var model = new Act
        {
            PerformanceId = performanceId,
            Number = nextNumber + 1,
            Position = nextPosition + 1
        };

        ViewBag.PerformanceTitle = performance.Title;

        return View(model);
    }

    // POST: Acts/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(
            "PerformanceId,Number,Title," +
            "Description,Position")]
        Act act)
    {
        NormalizeAct(act);

        await ValidateActAsync(act);

        if (!ModelState.IsValid)
        {
            await LoadPerformanceTitleAsync(
                act.PerformanceId);

            return View(act);
        }

        _context.Acts.Add(act);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                nameof(act.Number),
                "У цій виставі вже існує дія з таким номером.");

            await LoadPerformanceTitleAsync(
                act.PerformanceId);

            return View(act);
        }

        TempData["SuccessMessage"] =
            $"Дію «{act.DisplayName}» успішно створено.";

        return RedirectToAction(
            nameof(Index),
            new
            {
                performanceId = act.PerformanceId
            });
    }

    // GET: Acts/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var act = await _context.Acts
            .AsNoTracking()
            .Include(item => item.Performance)
            .FirstOrDefaultAsync(item =>
                item.Id == id.Value);

        if (act == null)
        {
            return NotFound();
        }

        ViewBag.PerformanceTitle =
            act.Performance.Title;

        return View(act);
    }

    // POST: Acts/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind(
            "Id,PerformanceId,Number,Title," +
            "Description,Position")]
        Act act)
    {
        if (id != act.Id)
        {
            return NotFound();
        }

        NormalizeAct(act);

        await ValidateActAsync(
            act,
            excludedActId: id);

        if (!ModelState.IsValid)
        {
            await LoadPerformanceTitleAsync(
                act.PerformanceId);

            return View(act);
        }

        var existingAct = await _context.Acts
            .FirstOrDefaultAsync(item =>
                item.Id == id);

        if (existingAct == null)
        {
            return NotFound();
        }

        existingAct.Number = act.Number;
        existingAct.Title = act.Title;
        existingAct.Description = act.Description;
        existingAct.Position = act.Position;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                nameof(act.Number),
                "У цій виставі вже існує дія з таким номером.");

            await LoadPerformanceTitleAsync(
                act.PerformanceId);

            return View(act);
        }

        TempData["SuccessMessage"] =
            $"Дію «{existingAct.DisplayName}» успішно оновлено.";

        return RedirectToAction(
            nameof(Index),
            new
            {
                performanceId =
                    existingAct.PerformanceId
            });
    }

    // GET: Acts/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var act = await _context.Acts
            .AsNoTracking()
            .Include(item => item.Performance)
            .Include(item => item.Scenes)
            .FirstOrDefaultAsync(item =>
                item.Id == id.Value);

        if (act == null)
        {
            return NotFound();
        }

        return View(act);
    }

    // POST: Acts/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(
        int id)
    {
        var act = await _context.Acts
            .Include(item => item.Scenes)
            .FirstOrDefaultAsync(item =>
                item.Id == id);

        if (act == null)
        {
            return NotFound();
        }

        var performanceId = act.PerformanceId;
        var actName = act.DisplayName;

        _context.Acts.Remove(act);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"Дію «{actName}» та її сцени видалено.";

        return RedirectToAction(
            nameof(Index),
            new
            {
                performanceId
            });
    }

    private async Task ValidateActAsync(
        Act act,
        int? excludedActId = null)
    {
        var performanceExists =
            await _context.Performances.AnyAsync(
                performance =>
                    performance.Id ==
                    act.PerformanceId);

        if (!performanceExists)
        {
            ModelState.AddModelError(
                nameof(act.PerformanceId),
                "Обрана вистава не існує.");
        }

        if (act.PerformanceId <= 0 ||
            act.Number <= 0)
        {
            return;
        }

        var duplicateExists =
            await _context.Acts.AnyAsync(
                existing =>
                    existing.PerformanceId ==
                        act.PerformanceId &&

                    existing.Number ==
                        act.Number &&

                    (!excludedActId.HasValue ||
                     existing.Id !=
                        excludedActId.Value));

        if (duplicateExists)
        {
            ModelState.AddModelError(
                nameof(act.Number),
                "У цій виставі вже існує дія з таким номером.");
        }
    }

    private async Task LoadPerformanceTitleAsync(
        int performanceId)
    {
        ViewBag.PerformanceTitle =
            await _context.Performances
                .AsNoTracking()
                .Where(performance =>
                    performance.Id ==
                    performanceId)
                .Select(performance =>
                    performance.Title)
                .FirstOrDefaultAsync();
    }

    private static void NormalizeAct(Act act)
    {
        act.Title = NormalizeOptionalText(
            act.Title);

        act.Description = NormalizeOptionalText(
            act.Description);
    }

    private static string? NormalizeOptionalText(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}