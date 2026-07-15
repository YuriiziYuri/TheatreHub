using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheatreHub.Data;
using TheatreHub.Models;
using TheatreHub.Models.Enums;
using TheatreHub.ViewModels;

namespace TheatreHub.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(
        ILogger<HomeController> logger,
        ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var now = DateTime.Now;

        var performances = await _context.Performances
            .AsNoTracking()
            .OrderBy(performance =>
                performance.Title)
            .ToListAsync();

        var performancesCount =
            performances.Count;

        var performancesInPreparationCount =
            performances.Count(performance =>
                performance.Status != PerformanceStatus.Completed &&
                performance.Status != PerformanceStatus.Cancelled);

        var activeActorsCount =
            await _context.Actors
                .AsNoTracking()
                .CountAsync();

        var futureRehearsals = await _context.Rehearsals
            .AsNoTracking()
            .Include(rehearsal => rehearsal.Performance)
            .Include(rehearsal => rehearsal.Act)
            .Include(rehearsal => rehearsal.Scene)
            .Include(rehearsal => rehearsal.Hall)
                .ThenInclude(hall => hall.Venue)
            .Where(rehearsal =>
                rehearsal.StartDateTime >= now)
            .OrderBy(rehearsal =>
                rehearsal.StartDateTime)
            .ToListAsync();

        var upcomingRehearsalsRaw = futureRehearsals
            .Where(rehearsal =>
                rehearsal.Status.ToString() != "Cancelled")
            .Take(5)
            .ToList();

        var upcomingRehearsalsCount = futureRehearsals
            .Count(rehearsal =>
                rehearsal.Status.ToString() != "Cancelled");

        var openTasksQuery = _context.TheatreTasks
            .AsNoTracking()
            .Where(task =>
                task.Status != TheatreTaskStatus.Done &&
                task.Status != TheatreTaskStatus.Cancelled);

        var openTasksCount =
            await openTasksQuery.CountAsync();

        var overdueTasksCount =
            await openTasksQuery.CountAsync(task =>
                task.Deadline.HasValue &&
                task.Deadline.Value < today);

        var criticalTasksCount =
            await openTasksQuery.CountAsync(task =>
                task.Priority == TheatreTaskPriority.Critical);

        var attentionTasksRaw = await openTasksQuery
            .Include(task => task.Performance)
            .Include(task => task.Act)
            .Include(task => task.Scene)
            .Where(task =>
                task.Priority == TheatreTaskPriority.Critical ||
                (task.Deadline.HasValue &&
                 task.Deadline.Value < today))
            .OrderBy(task =>
                task.Deadline ?? DateTime.MaxValue)
            .ThenByDescending(task =>
                task.Priority)
            .Take(6)
            .ToListAsync();

        var activeProductionItemsQuery = _context.ProductionItems
            .AsNoTracking()
            .Where(item =>
                item.Status != ProductionItemStatus.Ready &&
                item.Status != ProductionItemStatus.Cancelled);

        var problemProductionItemsCount =
            await activeProductionItemsQuery.CountAsync(item =>
                item.Status == ProductionItemStatus.Missing ||
                (item.NeededBy.HasValue &&
                 item.NeededBy.Value < today));

        var problemProductionItemsRaw =
            await activeProductionItemsQuery
                .Include(item => item.Performance)
                .Include(item => item.Act)
                .Include(item => item.Scene)
                .Where(item =>
                    item.Status == ProductionItemStatus.Missing ||
                    (item.NeededBy.HasValue &&
                     item.NeededBy.Value < today))
                .OrderBy(item =>
                    item.NeededBy ?? DateTime.MaxValue)
                .ThenBy(item =>
                    item.Type)
                .Take(6)
                .ToListAsync();

        var attentionPerformances =
            new List<DashboardPerformanceItemViewModel>();

        foreach (var performance in performances
            .Where(performance =>
                performance.Status != PerformanceStatus.Completed &&
                performance.Status != PerformanceStatus.Cancelled)
            .Take(10))
        {
            var rolesWithoutMainCast =
                await _context.CharacterRoles
                    .AsNoTracking()
                    .Where(role =>
                        role.PerformanceId == performance.Id)
                    .CountAsync(role =>
                        !role.Assignments.Any(assignment =>
                            assignment.IsCurrent &&
                            assignment.Status == RoleAssignmentStatus.Approved &&
                            assignment.CastType == CastType.Main));

            var performanceOpenTasks =
                await _context.TheatreTasks
                    .AsNoTracking()
                    .CountAsync(task =>
                        task.PerformanceId == performance.Id &&
                        task.Status != TheatreTaskStatus.Done &&
                        task.Status != TheatreTaskStatus.Cancelled);

            var performanceProblemItems =
                await _context.ProductionItems
                    .AsNoTracking()
                    .CountAsync(item =>
                        item.PerformanceId == performance.Id &&
                        item.Status != ProductionItemStatus.Ready &&
                        item.Status != ProductionItemStatus.Cancelled &&
                        (item.Status == ProductionItemStatus.Missing ||
                         (item.NeededBy.HasValue &&
                          item.NeededBy.Value < today)));

            var nextRehearsal = await _context.Rehearsals
                .AsNoTracking()
                .Include(rehearsal => rehearsal.Act)
                .Include(rehearsal => rehearsal.Scene)
                .Where(rehearsal =>
                    rehearsal.PerformanceId == performance.Id &&
                    rehearsal.StartDateTime >= now)
                .OrderBy(rehearsal =>
                    rehearsal.StartDateTime)
                .FirstOrDefaultAsync();

            if (rolesWithoutMainCast > 0 ||
                performanceOpenTasks > 0 ||
                performanceProblemItems > 0)
            {
                attentionPerformances.Add(
                    new DashboardPerformanceItemViewModel
                    {
                        Id = performance.Id,
                        Title = performance.Title,
                        StatusText =
                            GetPerformanceStatusText(performance.Status),
                        PremiereDate = performance.PremiereDate,
                        RolesWithoutMainCast = rolesWithoutMainCast,
                        OpenTasksCount = performanceOpenTasks,
                        ProblemProductionItemsCount =
                            performanceProblemItems,
                        NextRehearsalText = nextRehearsal == null
                            ? null
                            : $"{nextRehearsal.StartDateTime:dd.MM.yyyy HH:mm} — {GetRehearsalTargetText(nextRehearsal)}"
                    });
            }
        }

        var model = new DashboardViewModel
        {
            PerformancesCount = performancesCount,

            PerformancesInPreparationCount =
                performancesInPreparationCount,

            ActiveActorsCount = activeActorsCount,

            UpcomingRehearsalsCount =
                upcomingRehearsalsCount,

            OpenTasksCount = openTasksCount,

            OverdueTasksCount = overdueTasksCount,

            CriticalTasksCount = criticalTasksCount,

            ProblemProductionItemsCount =
                problemProductionItemsCount,

            UpcomingRehearsals = upcomingRehearsalsRaw
                .Select(rehearsal =>
                    new DashboardRehearsalItemViewModel
                    {
                        Id = rehearsal.Id,
                        StartDateTime = rehearsal.StartDateTime,
                        EndDateTime = rehearsal.EndDateTime,
                        PerformanceTitle =
                            rehearsal.Performance.Title,
                        TargetText =
                            GetRehearsalTargetText(rehearsal),
                        HallText =
                            GetHallText(rehearsal),
                        StatusText =
                            GetRehearsalStatusText(rehearsal.Status)
                    })
                .ToList(),

            AttentionTasks = attentionTasksRaw
                .Select(task =>
                    new DashboardTaskItemViewModel
                    {
                        Id = task.Id,
                        Title = task.Title,
                        PerformanceTitle =
                            task.Performance.Title,
                        TargetText =
                            GetTaskTargetText(task),
                        PriorityText =
                            GetTaskPriorityText(task.Priority),
                        Deadline = task.Deadline,
                        IsOverdue =
                            task.Deadline.HasValue &&
                            task.Deadline.Value < today
                    })
                .ToList(),

            ProblemProductionItems = problemProductionItemsRaw
                .Select(item =>
                    new DashboardProductionItemViewModel
                    {
                        Id = item.Id,
                        Name = item.Name,
                        PerformanceTitle =
                            item.Performance.Title,
                        TypeText =
                            GetProductionItemTypeText(item.Type),
                        StatusText =
                            GetProductionItemStatusText(item.Status),
                        NeededBy = item.NeededBy,
                        IsOverdue =
                            item.NeededBy.HasValue &&
                            item.NeededBy.Value < today
                    })
                .ToList(),

            AttentionPerformances = attentionPerformances
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult Error()
    {
        return View(
            new ErrorViewModel
            {
                RequestId =
                    Activity.Current?.Id ??
                    HttpContext.TraceIdentifier
            });
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

    private static string GetPerformanceStatusText(
        PerformanceStatus status)
    {
        return status switch
        {
            PerformanceStatus.Idea => "Ідея",
            PerformanceStatus.Planning => "Планування",
            PerformanceStatus.Casting => "Кастинг",
            PerformanceStatus.RehearsalPeriod => "Репетиційний період",
            PerformanceStatus.TechnicalPreparation => "Технічна підготовка",
            PerformanceStatus.ReadyForPremiere => "Готова до прем’єри",
            PerformanceStatus.InRepertoire => "У репертуарі",
            PerformanceStatus.Completed => "Завершено",
            PerformanceStatus.Cancelled => "Скасовано",
            _ => status.ToString()
        };
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