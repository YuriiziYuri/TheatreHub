using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheatreHub.Data;
using TheatreHub.Models;
using TheatreHub.Models.Enums;
using TheatreHub.ViewModels;

namespace TheatreHub.Controllers;

public class CalendarController : Controller
{
    private readonly ApplicationDbContext _context;

    public CalendarController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(
        DateTime? startDate,
        DateTime? endDate,
        int? performanceId,
        int? actorId,
        int? hallId,
        string? eventType,
        bool includeCompleted = false,
        bool includeCancelled = false)
    {
        var normalizedEventType =
            NormalizeEventType(eventType);

        var range =
            ResolveCalendarRange(
                startDate,
                endDate);

        var queryStart =
            range.StartDate;

        var queryEnd =
            range.EndDate.AddDays(1);

        var calendarItems =
            new List<TheatreCalendarItemViewModel>();

        List<int> performanceIdsForActor = [];

        if (actorId.HasValue)
        {
            performanceIdsForActor =
                await _context.RoleAssignments
                    .AsNoTracking()
                    .Where(assignment =>
                        assignment.ActorId == actorId.Value &&
                        assignment.IsCurrent &&
                        assignment.Status == RoleAssignmentStatus.Approved)
                    .Select(assignment =>
                        assignment.CharacterRole.PerformanceId)
                    .Distinct()
                    .ToListAsync();
        }

        if (IsEventTypeSelected(normalizedEventType, "rehearsal"))
        {
            var rehearsalItems =
                await LoadRehearsalItemsAsync(
                    queryStart,
                    queryEnd,
                    performanceId,
                    actorId,
                    hallId,
                    includeCompleted,
                    includeCancelled);

            calendarItems.AddRange(rehearsalItems);
        }

        if (IsEventTypeSelected(normalizedEventType, "show"))
        {
            var showItems =
                await LoadShowItemsAsync(
                    queryStart,
                    queryEnd,
                    performanceId,
                    actorId,
                    hallId,
                    performanceIdsForActor,
                    includeCompleted,
                    includeCancelled);

            calendarItems.AddRange(showItems);
        }

        if (IsEventTypeSelected(normalizedEventType, "task") &&
            !hallId.HasValue)
        {
            var taskItems =
                await LoadTaskItemsAsync(
                    queryStart,
                    queryEnd,
                    performanceId,
                    actorId,
                    performanceIdsForActor,
                    includeCompleted,
                    includeCancelled);

            calendarItems.AddRange(taskItems);
        }

        if (IsEventTypeSelected(normalizedEventType, "premiere") &&
            !hallId.HasValue)
        {
            var premiereItems =
                await LoadPremiereItemsAsync(
                    queryStart,
                    queryEnd,
                    performanceId,
                    actorId,
                    performanceIdsForActor);

            calendarItems.AddRange(premiereItems);
        }

        calendarItems = calendarItems
            .OrderBy(item =>
                item.StartDateTime)
            .ThenBy(item =>
                item.SortOrder)
            .ThenBy(item =>
                item.Title)
            .ToList();

        var days = new List<TheatreCalendarDayViewModel>();

        for (var date = range.StartDate;
             date <= range.EndDate;
             date = date.AddDays(1))
        {
            var dayItems = calendarItems
                .Where(item =>
                    item.StartDateTime.Date == date.Date)
                .OrderBy(item =>
                    item.IsAllDay ? 0 : 1)
                .ThenBy(item =>
                    item.StartDateTime)
                .ThenBy(item =>
                    item.SortOrder)
                .ToList();

            days.Add(
                new TheatreCalendarDayViewModel
                {
                    Date = date,
                    Items = dayItems
                });
        }

        var model =
            new TheatreCalendarViewModel
            {
                StartDate = range.StartDate,
                EndDate = range.EndDate,
                PerformanceId = performanceId,
                ActorId = actorId,
                HallId = hallId,
                EventType = normalizedEventType,
                IncludeCompleted = includeCompleted,
                IncludeCancelled = includeCancelled,
                Days = days,

                TotalEvents =
                    calendarItems.Count,

                RehearsalsCount =
                    calendarItems.Count(item =>
                        item.EventType == "rehearsal"),

                ShowsCount =
                    calendarItems.Count(item =>
                        item.EventType == "show"),

                TasksCount =
                    calendarItems.Count(item =>
                        item.EventType == "task"),

                PremieresCount =
                    calendarItems.Count(item =>
                        item.EventType == "premiere"),

                OverdueTasksCount =
                    calendarItems.Count(item =>
                        item.EventType == "task" &&
                        item.IsOverdue),

                Performances =
                    await _context.Performances
                        .AsNoTracking()
                        .OrderBy(performance =>
                            performance.Title)
                        .ToListAsync(),

                Actors =
                    await _context.Actors
                        .AsNoTracking()
                        .OrderBy(actor =>
                            actor.LastName)
                        .ThenBy(actor =>
                            actor.FirstName)
                        .ToListAsync(),

                Halls =
                    await _context.Halls
                        .AsNoTracking()
                        .Include(hall => hall.Venue!)
                        .Where(hall =>
                            hall.IsActive &&
                            hall.Venue!.IsActive)
                        .OrderBy(hall =>
                            hall.Venue!.Name)
                        .ThenBy(hall =>
                            hall.Name)
                        .ToListAsync()
            };

        return View(model);
    }

    private async Task<List<TheatreCalendarItemViewModel>> LoadRehearsalItemsAsync(
        DateTime queryStart,
        DateTime queryEnd,
        int? performanceId,
        int? actorId,
        int? hallId,
        bool includeCompleted,
        bool includeCancelled)
    {
        var query = _context.Rehearsals
            .AsNoTracking()
            .Include(rehearsal => rehearsal.Performance)
            .Include(rehearsal => rehearsal.Act)
            .Include(rehearsal => rehearsal.Scene)
            .Include(rehearsal => rehearsal.Hall)
                .ThenInclude(hall => hall!.Venue!)
            .Include(rehearsal => rehearsal.Participants)
            .Where(rehearsal =>
                rehearsal.StartDateTime < queryEnd &&
                rehearsal.EndDateTime >= queryStart);

        if (performanceId.HasValue)
        {
            query = query.Where(rehearsal =>
                rehearsal.PerformanceId == performanceId.Value);
        }

        if (actorId.HasValue)
        {
            query = query.Where(rehearsal =>
                rehearsal.Participants.Any(participant =>
                    participant.ActorId == actorId.Value));
        }

        if (hallId.HasValue)
        {
            query = query.Where(rehearsal =>
                rehearsal.HallId == hallId.Value);
        }

        var rehearsals = await query
            .OrderBy(rehearsal =>
                rehearsal.StartDateTime)
            .ToListAsync();

        return rehearsals
            .Where(rehearsal =>
                includeCancelled ||
                !IsRehearsalCancelled(rehearsal.Status))
            .Where(rehearsal =>
                includeCompleted ||
                !IsRehearsalCompleted(rehearsal.Status))
            .Select(rehearsal =>
                new TheatreCalendarItemViewModel
                {
                    Id = rehearsal.Id,
                    EventType = "rehearsal",
                    EventTypeText = "Репетиція",
                    BadgeClass = "text-bg-primary",
                    ControllerName = "Rehearsals",
                    ActionName = "Details",
                    Title = rehearsal.Performance.Title,
                    Subtitle = GetRehearsalTargetText(rehearsal),
                    Details = $"Учасників: {rehearsal.Participants.Count}",
                    LocationText = GetRehearsalHallText(rehearsal),
                    StatusText = GetRehearsalStatusText(rehearsal.Status),
                    StartDateTime = rehearsal.StartDateTime,
                    EndDateTime = rehearsal.EndDateTime,
                    IsAllDay = false,
                    IsOverdue = false,
                    SortOrder = 20
                })
            .ToList();
    }

    private async Task<List<TheatreCalendarItemViewModel>> LoadShowItemsAsync(
        DateTime queryStart,
        DateTime queryEnd,
        int? performanceId,
        int? actorId,
        int? hallId,
        List<int> performanceIdsForActor,
        bool includeCompleted,
        bool includeCancelled)
    {
        var query = _context.PerformanceShows
            .AsNoTracking()
            .Include(show => show.Performance)
            .Include(show => show.Hall)
                .ThenInclude(hall => hall!.Venue!)
            .Where(show =>
                show.StartDateTime < queryEnd &&
                show.EndDateTime >= queryStart);

        if (performanceId.HasValue)
        {
            query = query.Where(show =>
                show.PerformanceId == performanceId.Value);
        }

        if (actorId.HasValue)
        {
            query = query.Where(show =>
                performanceIdsForActor.Contains(show.PerformanceId));
        }

        if (hallId.HasValue)
        {
            query = query.Where(show =>
                show.HallId == hallId.Value);
        }

        var shows = await query
            .OrderBy(show =>
                show.StartDateTime)
            .ToListAsync();

        return shows
            .Where(show =>
                includeCancelled ||
                show.Status != PerformanceShowStatus.Cancelled)
            .Where(show =>
                includeCompleted ||
                show.Status != PerformanceShowStatus.Completed)
            .Select(show =>
                new TheatreCalendarItemViewModel
                {
                    Id = show.Id,
                    EventType = "show",
                    EventTypeText = "Показ",
                    BadgeClass = "text-bg-success",
                    ControllerName = "PerformanceShows",
                    ActionName = "Details",
                    Title = show.Performance.Title,
                    Subtitle = GetShowTypeText(show.Type),
                    Details = GetAudienceText(show),
                    LocationText = GetShowLocationText(show),
                    StatusText = GetShowStatusText(show.Status),
                    StartDateTime = show.StartDateTime,
                    EndDateTime = show.EndDateTime,
                    IsAllDay = false,
                    IsOverdue = false,
                    SortOrder = 30
                })
            .ToList();
    }

    private async Task<List<TheatreCalendarItemViewModel>> LoadTaskItemsAsync(
        DateTime queryStart,
        DateTime queryEnd,
        int? performanceId,
        int? actorId,
        List<int> performanceIdsForActor,
        bool includeCompleted,
        bool includeCancelled)
    {
        var query = _context.TheatreTasks
            .AsNoTracking()
            .Include(task => task.Performance)
            .Include(task => task.Act)
            .Include(task => task.Scene)
            .Where(task =>
                task.Deadline.HasValue &&
                task.Deadline.Value >= queryStart &&
                task.Deadline.Value < queryEnd);

        if (performanceId.HasValue)
        {
            query = query.Where(task =>
                task.PerformanceId == performanceId.Value);
        }

        if (actorId.HasValue)
        {
            query = query.Where(task =>
                performanceIdsForActor.Contains(task.PerformanceId));
        }

        var tasks = await query
            .OrderBy(task =>
                task.Deadline)
            .ToListAsync();

        var today = DateTime.Today;

        return tasks
            .Where(task =>
                includeCancelled ||
                task.Status != TheatreTaskStatus.Cancelled)
            .Where(task =>
                includeCompleted ||
                task.Status != TheatreTaskStatus.Done)
            .Select(task =>
            {
                var deadlineDate =
                    task.Deadline!.Value.Date;

                var isOverdue =
                    deadlineDate < today &&
                    task.Status != TheatreTaskStatus.Done &&
                    task.Status != TheatreTaskStatus.Cancelled;

                return new TheatreCalendarItemViewModel
                {
                    Id = task.Id,
                    EventType = "task",
                    EventTypeText = "Завдання",
                    BadgeClass = isOverdue
                        ? "text-bg-danger"
                        : GetTaskPriorityBadgeClass(task.Priority),
                    ControllerName = "TheatreTasks",
                    ActionName = "Details",
                    Title = task.Title,
                    Subtitle = task.Performance.Title,
                    Details = GetTaskTargetText(task),
                    LocationText = string.IsNullOrWhiteSpace(task.ResponsibleName)
                        ? "Відповідального не вказано"
                        : $"Відповідальний: {task.ResponsibleName}",
                    StatusText = GetTaskStatusText(task.Status),
                    StartDateTime = deadlineDate.AddHours(17),
                    EndDateTime = null,
                    IsAllDay = true,
                    IsOverdue = isOverdue,
                    SortOrder = isOverdue ? 0 : 10
                };
            })
            .ToList();
    }

    private async Task<List<TheatreCalendarItemViewModel>> LoadPremiereItemsAsync(
        DateTime queryStart,
        DateTime queryEnd,
        int? performanceId,
        int? actorId,
        List<int> performanceIdsForActor)
    {
        var query = _context.Performances
            .AsNoTracking()
            .AsQueryable();

        if (performanceId.HasValue)
        {
            query = query.Where(performance =>
                performance.Id == performanceId.Value);
        }

        if (actorId.HasValue)
        {
            query = query.Where(performance =>
                performanceIdsForActor.Contains(performance.Id));
        }

        var performances =
            await query
                .OrderBy(performance =>
                    performance.Title)
                .ToListAsync();

        return performances
            .Select(performance =>
                new
                {
                    Performance = performance,
                    PremiereDate = GetPremiereDate(performance)
                })
            .Where(item =>
                item.PremiereDate.HasValue &&
                item.PremiereDate.Value >= queryStart &&
                item.PremiereDate.Value < queryEnd)
            .Select(item =>
                new TheatreCalendarItemViewModel
                {
                    Id = item.Performance.Id,
                    EventType = "premiere",
                    EventTypeText = "Прем’єра",
                    BadgeClass = "text-bg-dark",
                    ControllerName = "Performances",
                    ActionName = "Details",
                    Title = item.Performance.Title,
                    Subtitle = "Дата прем’єри вистави",
                    Details = string.IsNullOrWhiteSpace(item.Performance.Genre)
                        ? "Жанр не вказано"
                        : item.Performance.Genre,
                    LocationText = string.Empty,
                    StatusText = item.Performance.Status.ToString(),
                    StartDateTime = item.PremiereDate!.Value.Date.AddHours(18),
                    EndDateTime = null,
                    IsAllDay = true,
                    IsOverdue = false,
                    SortOrder = 5
                })
            .ToList();
    }

    private static (DateTime StartDate, DateTime EndDate) ResolveCalendarRange(
        DateTime? startDate,
        DateTime? endDate)
    {
        var today =
            DateTime.Today;

        var start =
            startDate?.Date ?? today;

        var end =
            endDate?.Date ?? today.AddDays(14);

        if (end < start)
        {
            end = start.AddDays(14);
        }

        return (start, end);
    }

    private static string NormalizeEventType(
        string? eventType)
    {
        return eventType switch
        {
            "rehearsal" => "rehearsal",
            "show" => "show",
            "task" => "task",
            "premiere" => "premiere",
            _ => "all"
        };
    }

    private static bool IsEventTypeSelected(
        string selectedType,
        string currentType)
    {
        return selectedType == "all" ||
               selectedType == currentType;
    }

    private static bool IsRehearsalCancelled(
        RehearsalStatus status)
    {
        return status.ToString() == "Cancelled";
    }

    private static bool IsRehearsalCompleted(
        RehearsalStatus status)
    {
        return status.ToString() is "Done" or "Completed";
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

    private static string GetRehearsalHallText(
        Rehearsal rehearsal)
    {
        if (rehearsal.Hall == null)
        {
            return "Зал не вказано";
        }

        return rehearsal.Hall.Venue == null
            ? rehearsal.Hall.Name
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
            "Completed" => "Проведено",
            "Cancelled" => "Скасовано",
            _ => status.ToString()
        };
    }

    private static string GetShowTypeText(
        PerformanceShowType type)
    {
        return type switch
        {
            PerformanceShowType.Premiere => "Прем’єра",
            PerformanceShowType.Regular => "Звичайний показ",
            PerformanceShowType.Touring => "Виїзний показ",
            PerformanceShowType.Closed => "Закритий показ",
            PerformanceShowType.Charity => "Благодійний показ",
            PerformanceShowType.Other => "Інший показ",
            _ => type.ToString()
        };
    }

    private static string GetShowLocationText(
        PerformanceShow show)
    {
        if (show.Hall != null)
        {
            return show.Hall.Venue == null
                ? show.Hall.Name
                : $"{show.Hall.Venue.Name} — {show.Hall.Name}";
        }

        return string.IsNullOrWhiteSpace(show.ExternalLocation)
            ? "Локацію не вказано"
            : show.ExternalLocation;
    }

    private static string GetShowStatusText(
        PerformanceShowStatus status)
    {
        return status switch
        {
            PerformanceShowStatus.Planned => "Заплановано",
            PerformanceShowStatus.Confirmed => "Підтверджено",
            PerformanceShowStatus.Completed => "Проведено",
            PerformanceShowStatus.Cancelled => "Скасовано",
            PerformanceShowStatus.Postponed => "Перенесено",
            _ => status.ToString()
        };
    }

    private static string GetAudienceText(
        PerformanceShow show)
    {
        if (!show.ExpectedAudienceCount.HasValue &&
            !show.ActualAudienceCount.HasValue)
        {
            return "Кількість глядачів не вказано";
        }

        return
            $"Глядачі: очікувано {show.ExpectedAudienceCount?.ToString() ?? "—"}, фактично {show.ActualAudienceCount?.ToString() ?? "—"}";
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

    private static string GetTaskPriorityBadgeClass(
        TheatreTaskPriority priority)
    {
        return priority switch
        {
            TheatreTaskPriority.Critical => "text-bg-danger",
            TheatreTaskPriority.High => "text-bg-warning text-dark",
            TheatreTaskPriority.Normal => "text-bg-secondary",
            TheatreTaskPriority.Low => "text-bg-light text-dark",
            _ => "text-bg-secondary"
        };
    }

    private static DateTime? GetPremiereDate(
        Performance performance)
    {
        var property =
            typeof(Performance)
                .GetProperty("PremiereDate");

        var value =
            property?.GetValue(performance);

        if (value is DateTime date &&
            date != default)
        {
            return date.Date;
        }

        return null;
    }
}