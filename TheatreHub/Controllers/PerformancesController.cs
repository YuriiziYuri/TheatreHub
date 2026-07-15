using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheatreHub.Data;
using TheatreHub.Models;
using TheatreHub.Models.Enums;
using TheatreHub.ViewModels;

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
            .Include(item => item.CharacterRoles)
                .ThenInclude(role => role.Assignments)
                    .ThenInclude(assignment => assignment.Actor)
            .Include(item => item.Rehearsals)
                .ThenInclude(rehearsal => rehearsal.Act)
            .Include(item => item.Rehearsals)
                .ThenInclude(rehearsal => rehearsal.Scene)
            .Include(item => item.Rehearsals)
                .ThenInclude(rehearsal => rehearsal.Hall)
                    .ThenInclude(hall => hall.Venue)
            .Include(item => item.Rehearsals)
                .ThenInclude(rehearsal => rehearsal.Participants)
            .FirstOrDefaultAsync(item => item.Id == id.Value);

        if (performance == null)
        {
            return NotFound();
        }

        SortStructure(performance);

        ViewBag.Tasks = await _context.TheatreTasks
    .AsNoTracking()
    .Include(task => task.Performance)
    .Include(task => task.Act)
    .Include(task => task.Scene)
    .Where(task =>
        task.PerformanceId == performance.Id)
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
        item.PerformanceId == performance.Id)
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

        ViewBag.PerformanceShows = await _context.PerformanceShows
    .AsNoTracking()
    .Include(show => show.Hall)
        .ThenInclude(hall => hall!.Venue!)
    .Where(show =>
        show.PerformanceId == performance.Id)
    .OrderBy(show =>
        show.StartDateTime)
    .ToListAsync();

        performance.CharacterRoles = performance.CharacterRoles
            .OrderBy(role => role.IsMainRole ? 0 : 1)
            .ThenBy(role => role.Name)
            .ToList();

        performance.Rehearsals = performance.Rehearsals
            .OrderBy(rehearsal => rehearsal.StartDateTime)
            .ToList();

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
            .Include(item => item.CharacterRoles)
            .Include(item => item.Rehearsals)
            .FirstOrDefaultAsync(item =>
                item.Id == id.Value);

        if (performance == null)
        {
            return NotFound();
        }

        ViewBag.PerformanceShowsCount =
            await _context.PerformanceShows
                .AsNoTracking()
                .CountAsync(show =>
                    show.PerformanceId == performance.Id);

        return View(performance);
    }

    // POST: Performances/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(
    int id)
    {
        var performance = await _context.Performances
            .Include(item => item.Acts)
            .Include(item => item.CharacterRoles)
            .Include(item => item.Rehearsals)
            .FirstOrDefaultAsync(item =>
                item.Id == id);

        if (performance == null)
        {
            return NotFound();
        }

        var hasPerformanceShows = await _context.PerformanceShows
    .AsNoTracking()
    .AnyAsync(show =>
        show.PerformanceId == id);

        if (hasPerformanceShows)
        {
            TempData["ErrorMessage"] =
                "Неможливо видалити виставу, тому що до неї прив’язані покази.";

            return RedirectToAction(
                nameof(Delete),
                new { id });
        }

        var hasTasks =
            await _context.TheatreTasks.AnyAsync(task =>
                task.PerformanceId == performance.Id);

        var hasProductionItems =
            await _context.ProductionItems.AnyAsync(item =>
                item.PerformanceId == performance.Id);

        if (performance.Acts.Any() ||
            performance.CharacterRoles.Any() ||
            performance.Rehearsals.Any() ||
            hasTasks ||
            hasProductionItems)
        {
            TempData["ErrorMessage"] =
                "Неможливо видалити виставу, бо вона має структуру, ролі, репетиції, завдання або постановочні елементи. Спочатку видаліть або перенесіть пов’язані дані.";

            return RedirectToAction(
                nameof(Details),
                new { id = performance.Id });
        }

        var performanceTitle = performance.Title;

        _context.Performances.Remove(performance);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"Виставу «{performanceTitle}» видалено.";

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Preparation(int? id)
{
    if (id == null)
    {
        return NotFound();
    }

    var performance = await _context.Performances
        .AsNoTracking()
        .FirstOrDefaultAsync(item =>
            item.Id == id.Value);

    if (performance == null)
    {
        return NotFound();
    }

    var today = DateTime.Today;
    var now = DateTime.Now;

    var roles = await _context.CharacterRoles
        .AsNoTracking()
        .Include(role => role.Assignments)
            .ThenInclude(assignment => assignment.Actor)
        .Where(role =>
            role.PerformanceId == performance.Id)
        .OrderBy(role =>
            role.IsMainRole ? 0 : 1)
        .ThenBy(role =>
            role.Name)
        .ToListAsync();

    var scenes = await _context.Scenes
        .AsNoTracking()
        .Include(scene => scene.Act)
        .Include(scene => scene.SceneRoles)
        .Where(scene =>
            scene.Act.PerformanceId == performance.Id)
        .OrderBy(scene =>
            scene.Act.Position)
        .ThenBy(scene =>
            scene.Act.Number)
        .ThenBy(scene =>
            scene.Position)
        .ThenBy(scene =>
            scene.Number)
        .ToListAsync();

    var rehearsals = await _context.Rehearsals
        .AsNoTracking()
        .Include(rehearsal => rehearsal.Act)
        .Include(rehearsal => rehearsal.Scene)
        .Include(rehearsal => rehearsal.Hall)
            .ThenInclude(hall => hall.Venue)
        .Where(rehearsal =>
            rehearsal.PerformanceId == performance.Id)
        .OrderBy(rehearsal =>
            rehearsal.StartDateTime)
        .ToListAsync();

    var tasks = await _context.TheatreTasks
        .AsNoTracking()
        .Include(task => task.Act)
        .Include(task => task.Scene)
        .Where(task =>
            task.PerformanceId == performance.Id)
        .OrderBy(task =>
            task.Deadline ?? DateTime.MaxValue)
        .ToListAsync();

    var productionItems = await _context.ProductionItems
        .AsNoTracking()
        .Include(item => item.Act)
        .Include(item => item.Scene)
        .Where(item =>
            item.PerformanceId == performance.Id)
        .OrderBy(item =>
            item.NeededBy ?? DateTime.MaxValue)
        .ToListAsync();

    var rolesWithMainCast = roles.Count(role =>
        role.Assignments.Any(assignment =>
            assignment.IsCurrent &&
            assignment.Status == RoleAssignmentStatus.Approved &&
            assignment.CastType == CastType.Main));

    var rolesWithoutMainCast = roles
        .Where(role =>
            !role.Assignments.Any(assignment =>
                assignment.IsCurrent &&
                assignment.Status == RoleAssignmentStatus.Approved &&
                assignment.CastType == CastType.Main))
        .ToList();

    var rolesWithoutReserveCast = roles.Count(role =>
        !role.Assignments.Any(assignment =>
            assignment.IsCurrent &&
            assignment.Status == RoleAssignmentStatus.Approved &&
            assignment.CastType == CastType.Reserve));

    var scenesWithoutRoles = scenes
        .Where(scene =>
            !scene.SceneRoles.Any())
        .ToList();

    var completedRehearsals = rehearsals.Count(rehearsal =>
        rehearsal.Status.ToString() == "Done");

    var cancelledRehearsals = rehearsals.Count(rehearsal =>
        rehearsal.Status.ToString() == "Cancelled");

    var nextRehearsal = rehearsals
        .Where(rehearsal =>
            rehearsal.StartDateTime >= now &&
            rehearsal.Status.ToString() != "Cancelled")
        .OrderBy(rehearsal =>
            rehearsal.StartDateTime)
        .FirstOrDefault();

    var upcomingRehearsals = rehearsals
        .Where(rehearsal =>
            rehearsal.StartDateTime >= now &&
            rehearsal.Status.ToString() != "Cancelled")
        .OrderBy(rehearsal =>
            rehearsal.StartDateTime)
        .Take(5)
        .Select(rehearsal =>
            new PreparationRehearsalItemViewModel
            {
                Id = rehearsal.Id,
                StartDateTime = rehearsal.StartDateTime,
                EndDateTime = rehearsal.EndDateTime,
                TargetText = GetRehearsalTargetText(rehearsal),
                HallText = GetHallText(rehearsal),
                StatusText = GetRehearsalStatusText(rehearsal.Status)
            })
        .ToList();

    var openTasks = tasks
        .Where(task =>
            task.Status != TheatreTaskStatus.Done &&
            task.Status != TheatreTaskStatus.Cancelled)
        .ToList();

    var overdueTasks = openTasks
        .Where(task =>
            task.Deadline.HasValue &&
            task.Deadline.Value.Date < today)
        .ToList();

    var criticalOpenTasks = openTasks
        .Where(task =>
            task.Priority == TheatreTaskPriority.Critical)
        .ToList();

    var problemTasks = openTasks
        .Where(task =>
            criticalOpenTasks.Contains(task) ||
            overdueTasks.Contains(task))
        .OrderBy(task =>
            task.Deadline ?? DateTime.MaxValue)
        .ThenByDescending(task =>
            task.Priority)
        .Select(task =>
            new PreparationTaskItemViewModel
            {
                Id = task.Id,
                Title = task.Title,
                TargetText = GetTaskTargetText(task),
                StatusText = GetTaskStatusText(task.Status),
                PriorityText = GetTaskPriorityText(task.Priority),
                Deadline = task.Deadline,
                IsOverdue =
                    task.Deadline.HasValue &&
                    task.Deadline.Value.Date < today
            })
        .ToList();

    var activeProductionItems = productionItems
        .Where(item =>
            item.Status != ProductionItemStatus.Ready &&
            item.Status != ProductionItemStatus.Cancelled)
        .ToList();

    var overdueProductionItems = activeProductionItems
        .Where(item =>
            item.NeededBy.HasValue &&
            item.NeededBy.Value.Date < today)
        .ToList();

    var missingProductionItems = productionItems
        .Where(item =>
            item.Status == ProductionItemStatus.Missing)
        .ToList();

    var problemProductionItems = activeProductionItems
        .Where(item =>
            missingProductionItems.Contains(item) ||
            overdueProductionItems.Contains(item))
        .OrderBy(item =>
            item.NeededBy ?? DateTime.MaxValue)
        .ThenBy(item =>
            item.Type)
        .Select(item =>
            new PreparationProductionItemViewModel
            {
                Id = item.Id,
                Name = item.Name,
                TypeText = GetProductionItemTypeText(item.Type),
                TargetText = GetProductionItemTargetText(item),
                StatusText = GetProductionItemStatusText(item.Status),
                NeededBy = item.NeededBy,
                IsOverdue =
                    item.NeededBy.HasValue &&
                    item.NeededBy.Value.Date < today
            })
        .ToList();

    var model = new PerformancePreparationViewModel
    {
        PerformanceId = performance.Id,
        PerformanceTitle = performance.Title,
        PerformanceStatus = performance.Status,
        PremiereDate = performance.PremiereDate,

        RolesTotal = roles.Count,
        RolesWithMainCast = rolesWithMainCast,
        RolesWithoutMainCast = rolesWithoutMainCast.Count,
        RolesWithoutReserveCast = rolesWithoutReserveCast,

        RoleIssues = rolesWithoutMainCast
            .Select(role =>
                new PreparationIssueViewModel
                {
                    Title = role.Name,
                    Text = "Не призначено основного актора.",
                    Url = Url.Action(
                        "Details",
                        "CharacterRoles",
                        new { id = role.Id })
                })
            .ToList(),

        ScenesTotal = scenes.Count,
        ScenesWithRoles = scenes.Count - scenesWithoutRoles.Count,
        ScenesWithoutRoles = scenesWithoutRoles.Count,

        SceneIssues = scenesWithoutRoles
            .Select(scene =>
                new PreparationIssueViewModel
                {
                    Title =
                        $"{scene.Act.DisplayName} — Сцена {scene.Number}. {scene.Title}",
                    Text = "До сцени ще не прив’язані ролі.",
                    Url = Url.Action(
                        "Details",
                        "Scenes",
                        new { id = scene.Id })
                })
            .ToList(),

        RehearsalsTotal = rehearsals.Count,
        RehearsalsCompleted = completedRehearsals,
        RehearsalsCancelled = cancelledRehearsals,
        NextRehearsalText = nextRehearsal == null
            ? null
            : $"{nextRehearsal.StartDateTime:dd.MM.yyyy HH:mm} — {GetRehearsalTargetText(nextRehearsal)}",
        UpcomingRehearsals = upcomingRehearsals,

        TasksTotal = tasks.Count,
        TasksDone = tasks.Count(task =>
            task.Status == TheatreTaskStatus.Done),
        TasksInProgress = tasks.Count(task =>
            task.Status == TheatreTaskStatus.InProgress),
        TasksOverdue = overdueTasks.Count,
        CriticalOpenTasks = criticalOpenTasks.Count,
        ProblemTasks = problemTasks,

        ProductionItemsTotal = productionItems.Count,
        ProductionItemsReady = productionItems.Count(item =>
            item.Status == ProductionItemStatus.Ready),
        ProductionItemsInProgress = productionItems.Count(item =>
            item.Status == ProductionItemStatus.InProgress),
        ProductionItemsMissing = missingProductionItems.Count,
        ProductionItemsOverdue = overdueProductionItems.Count,
        ProblemProductionItems = problemProductionItems
    };

    if (model.RolesWithoutMainCast > 0)
    {
        model.AttentionItems.Add(
            $"Є ролі без основного актора: {model.RolesWithoutMainCast}.");
    }

    if (model.ScenesWithoutRoles > 0)
    {
        model.AttentionItems.Add(
            $"Є сцени без прив’язаних ролей: {model.ScenesWithoutRoles}.");
    }

    if (model.RehearsalsTotal == 0)
    {
        model.AttentionItems.Add(
            "Для вистави ще не створено жодної репетиції.");
    }

    if (model.TasksOverdue > 0)
    {
        model.AttentionItems.Add(
            $"Є прострочені завдання: {model.TasksOverdue}.");
    }

    if (model.CriticalOpenTasks > 0)
    {
        model.AttentionItems.Add(
            $"Є відкриті критичні завдання: {model.CriticalOpenTasks}.");
    }

    if (model.ProductionItemsMissing > 0)
    {
        model.AttentionItems.Add(
            $"Є проблемні або відсутні постановочні елементи: {model.ProductionItemsMissing}.");
    }

    if (model.ProductionItemsOverdue > 0)
    {
        model.AttentionItems.Add(
            $"Є прострочені постановочні елементи: {model.ProductionItemsOverdue}.");
    }

    return View(model);
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

    private static string GetRehearsalTargetText(
    Rehearsal rehearsal)
{
    if (rehearsal.Scene != null)
    {
        return
            $"Сцена {rehearsal.Scene.Number}. {rehearsal.Scene.Title}";
    }

    if (rehearsal.Act != null)
    {
        return rehearsal.Act.DisplayName;
    }

    return "Уся вистава";
}

private static string GetHallText(
    Rehearsal rehearsal)
{
    return rehearsal.Hall?.Venue == null
        ? "Зал не вказано"
        : $"{rehearsal.Hall.Venue.Name} — {rehearsal.Hall.Name}";
}

private static string GetRehearsalStatusText(
    RehearsalStatus status)
{
    return status.ToString() switch
    {
        "Planned" => "Заплановано",
        "Confirmed" => "Підтверджено",
        "Done" => "Проведено",
        "Cancelled" => "Скасовано",
        _ => status.ToString()
    };
}

private static string GetTaskTargetText(
    TheatreTask task)
{
    if (task.Scene != null)
    {
        return
            $"Сцена {task.Scene.Number}. {task.Scene.Title}";
    }

    if (task.Act != null)
    {
        return task.Act.DisplayName;
    }

    return "Уся вистава";
}

private static string GetTaskStatusText(
    TheatreTaskStatus status)
{
    return status switch
    {
        TheatreTaskStatus.Planned => "Заплановано",
        TheatreTaskStatus.InProgress => "У роботі",
        TheatreTaskStatus.Done => "Виконано",
        TheatreTaskStatus.Cancelled => "Скасовано",
        _ => status.ToString()
    };
}

private static string GetTaskPriorityText(
    TheatreTaskPriority priority)
{
    return priority switch
    {
        TheatreTaskPriority.Low => "Низький",
        TheatreTaskPriority.Normal => "Звичайний",
        TheatreTaskPriority.High => "Високий",
        TheatreTaskPriority.Critical => "Критичний",
        _ => priority.ToString()
    };
}

private static string GetProductionItemTargetText(
    ProductionItem item)
{
    if (item.Scene != null)
    {
        return
            $"Сцена {item.Scene.Number}. {item.Scene.Title}";
    }

    if (item.Act != null)
    {
        return item.Act.DisplayName;
    }

    return "Уся вистава";
}

private static string GetProductionItemTypeText(
    ProductionItemType type)
{
    return type switch
    {
        ProductionItemType.Prop => "Реквізит",
        ProductionItemType.Costume => "Костюм",
        ProductionItemType.Makeup => "Грим",
        ProductionItemType.Decoration => "Декорація",
        ProductionItemType.Equipment => "Обладнання",
        ProductionItemType.Other => "Інше",
        _ => type.ToString()
    };
}

private static string GetProductionItemStatusText(
    ProductionItemStatus status)
{
    return status switch
    {
        ProductionItemStatus.Needed => "Потрібно",
        ProductionItemStatus.InProgress => "У роботі",
        ProductionItemStatus.Ready => "Готово",
        ProductionItemStatus.Missing => "Проблема",
        ProductionItemStatus.Cancelled => "Скасовано",
        _ => status.ToString()
    };
}
}
