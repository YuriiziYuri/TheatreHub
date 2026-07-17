using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheatreHub.Data;
using TheatreHub.Models;
using TheatreHub.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using TheatreHub.Constants;
using TheatreHub.Services.ActionLogs;

namespace TheatreHub.Controllers;

[Authorize]
public class ActsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IUserActionLogService _actionLogService;

    public ActsController(
        ApplicationDbContext context,
        IUserActionLogService actionLogService)
    {
        _context = context;
        _actionLogService = actionLogService;
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
            .Include(item => item.Rehearsals)
                .ThenInclude(rehearsal => rehearsal.Scene)
            .Include(item => item.Rehearsals)
                .ThenInclude(rehearsal => rehearsal.Hall)
                    .ThenInclude(hall => hall.Venue)
            .Include(item => item.Rehearsals)
                .ThenInclude(rehearsal => rehearsal.Participants)
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
        ViewBag.Tasks = await _context.TheatreTasks
.AsNoTracking()
.Include(task => task.Performance)
.Include(task => task.Act)
.Include(task => task.Scene)
.Where(task =>
    task.ActId == act.Id)
.OrderBy(task =>
    task.Status == TheatreTaskStatus.Done ||
    task.Status == TheatreTaskStatus.Cancelled)
.ThenBy(task =>
    task.Deadline ?? DateTime.MaxValue)
.ThenByDescending(task =>
    task.Priority)
.ToListAsync();

        ViewBag.ProductionItems = await _context.ProductionItems
    .AsNoTracking()
    .Include(item => item.Performance)
    .Include(item => item.Act)
    .Include(item => item.Scene)
    .Where(item =>
        item.ActId == act.Id)
    .OrderBy(item =>
        item.Status == ProductionItemStatus.Ready ||
        item.Status == ProductionItemStatus.Cancelled)
    .ThenBy(item =>
        item.NeededBy ?? DateTime.MaxValue)
    .ThenBy(item =>
        item.Type)
    .ThenBy(item =>
        item.Name)
    .ToListAsync();

        act.Rehearsals = act.Rehearsals
            .OrderBy(rehearsal => rehearsal.StartDateTime)
            .ToList();

        return View(act);
    }

    // GET: Acts/Create?performanceId=5
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
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
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
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

        var performanceTitle = await _context.Performances
            .Where(performance =>
                performance.Id == act.PerformanceId)
            .Select(performance =>
                performance.Title)
            .FirstOrDefaultAsync();

        await _actionLogService.LogAsync(
            User,
            "Create",
            "Act",
            act.Id,
            act.DisplayName,
            $"Створено дію «{act.DisplayName}» у виставі «{performanceTitle}».");

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
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
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
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
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

        var performanceTitle = await _context.Performances
            .Where(performance =>
                performance.Id == existingAct.PerformanceId)
            .Select(performance =>
                performance.Title)
            .FirstOrDefaultAsync();

        await _actionLogService.LogAsync(
            User,
            "Edit",
            "Act",
            existingAct.Id,
            existingAct.DisplayName,
            $"Відредаговано дію «{existingAct.DisplayName}» у виставі «{performanceTitle}».");

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
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
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
                .ThenInclude(scene => scene.Rehearsals)
            .Include(item => item.Rehearsals)
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
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
    public async Task<IActionResult> DeleteConfirmed(
        int id)
    {
        var act = await _context.Acts
            .Include(item => item.Scenes)
                .ThenInclude(scene => scene.Rehearsals)
            .Include(item => item.Rehearsals)
            .FirstOrDefaultAsync(item =>
                item.Id == id);

        if (act == null)
        {
            return NotFound();
        }

        var hasScenes =
            act.Scenes.Any();

        var hasDirectRehearsals =
            act.Rehearsals.Any();

        var hasSceneRehearsals =
            act.Scenes.Any(scene =>
                scene.Rehearsals.Any());

        var hasTasks =
    await _context.TheatreTasks.AnyAsync(task =>
        task.ActId == act.Id);

        var hasProductionItems =
            await _context.ProductionItems.AnyAsync(item =>
                item.ActId == act.Id);

        if (hasScenes ||
            hasDirectRehearsals ||
            hasSceneRehearsals ||
            hasTasks ||
            hasProductionItems)
        {
            TempData["ErrorMessage"] =
                "Неможливо видалити дію, бо вона має сцени, репетиції, завдання або постановочні елементи. Спочатку видаліть або перенесіть ці дані.";

            return RedirectToAction(
                nameof(Details),
                new { id = act.Id });
        }

        var performanceId = act.PerformanceId;
        var actName = act.DisplayName;

        _context.Acts.Remove(act);
        await _context.SaveChangesAsync();

        await _actionLogService.LogAsync(
            User,
            "Delete",
            "Act",
            id,
            actName,
            $"Видалено дію «{actName}».");

        TempData["SuccessMessage"] =
            $"Дію «{actName}» видалено.";

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