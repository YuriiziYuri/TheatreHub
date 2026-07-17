using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TheatreHub.Constants;
using TheatreHub.Data;
using TheatreHub.Models;
using TheatreHub.Models.Enums;
using TheatreHub.ViewModels.Notifications;

namespace TheatreHub.Services.Notifications;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<NotificationsIndexViewModel> GetNotificationsAsync(
        ClaimsPrincipal user,
        bool includeRead = false)
    {
        var model = new NotificationsIndexViewModel();

        var currentUser =
            await _userManager.GetUserAsync(user);

        if (currentUser == null)
        {
            return model;
        }

        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var afterTomorrow = today.AddDays(2);

        var isAdminOrDirector =
            user.IsInRole(UserRoles.SystemAdmin)
            || user.IsInRole(UserRoles.GeneralDirector)
            || user.IsInRole(UserRoles.StageDirector);

        var canManageRehearsals =
            isAdminOrDirector
            || user.IsInRole(UserRoles.AssistantDirector);

        var canManageTasks =
            isAdminOrDirector
            || user.IsInRole(UserRoles.AssistantDirector)
            || user.IsInRole(UserRoles.ProductionStaff)
            || user.IsInRole(UserRoles.Playwright);

        var canManageProduction =
            isAdminOrDirector
            || user.IsInRole(UserRoles.ProductionStaff);

        var canViewShows =
            isAdminOrDirector
            || user.IsInRole(UserRoles.AssistantDirector)
            || user.IsInRole(UserRoles.Actor);

        var canViewFinance =
            user.IsInRole(UserRoles.SystemAdmin)
            || user.IsInRole(UserRoles.GeneralDirector)
            || user.IsInRole(UserRoles.FinanceManager);

        if (canManageTasks)
        {
            await AddTaskNotificationsAsync(model, today);
        }

        if (canManageRehearsals || user.IsInRole(UserRoles.Actor))
        {
            await AddRehearsalNotificationsAsync(
                model,
                today,
                tomorrow,
                afterTomorrow,
                currentUser,
                user);
        }

        if (canViewShows)
        {
            await AddShowNotificationsAsync(
                model,
                today,
                tomorrow,
                afterTomorrow,
                currentUser,
                user);
        }

        if (canManageProduction)
        {
            await AddProductionItemNotificationsAsync(model);
        }

        if (isAdminOrDirector)
        {
            await AddPremiereReadinessNotificationsAsync(model, today);
        }

        if (canViewFinance)
        {
            await AddFinanceNotificationsAsync(model, today);
        }

        await ApplyReadStatesAsync(model, currentUser.Id, includeRead);

        SortNotifications(model);

        return model;
    }

    public async Task MarkAsReadAsync(
        ClaimsPrincipal user,
        string notificationKey)
    {
        var currentUser =
            await _userManager.GetUserAsync(user);

        if (currentUser == null || string.IsNullOrWhiteSpace(notificationKey))
        {
            return;
        }

        var alreadyExists =
            await _context.NotificationReadStates
                .AnyAsync(state =>
                    state.UserId == currentUser.Id
                    && state.NotificationKey == notificationKey);

        if (alreadyExists)
        {
            return;
        }

        _context.NotificationReadStates.Add(new NotificationReadState
        {
            UserId = currentUser.Id,
            NotificationKey = notificationKey,
            ReadAt = DateTime.Now
        });

        await _context.SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync(
        ClaimsPrincipal user)
    {
        var currentUser =
            await _userManager.GetUserAsync(user);

        if (currentUser == null)
        {
            return;
        }

        var notifications =
            await GetNotificationsAsync(user, includeRead: true);

        var allKeys =
            notifications.CriticalNotifications
                .Concat(notifications.WarningNotifications)
                .Concat(notifications.InfoNotifications)
                .Select(notification => notification.Key)
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Distinct()
                .ToList();

        if (!allKeys.Any())
        {
            return;
        }

        var existingKeys =
            await _context.NotificationReadStates
                .Where(state => state.UserId == currentUser.Id
                                && allKeys.Contains(state.NotificationKey))
                .Select(state => state.NotificationKey)
                .ToListAsync();

        var newKeys =
            allKeys
                .Except(existingKeys)
                .ToList();

        foreach (var key in newKeys)
        {
            _context.NotificationReadStates.Add(new NotificationReadState
            {
                UserId = currentUser.Id,
                NotificationKey = key,
                ReadAt = DateTime.Now
            });
        }

        await _context.SaveChangesAsync();
    }

    private async Task AddTaskNotificationsAsync(
        NotificationsIndexViewModel model,
        DateTime today)
    {
        var tasks =
            await _context.TheatreTasks
                .Include(task => task.Performance)
                .Where(task => task.Status != TheatreTaskStatus.Done
                               && task.Status != TheatreTaskStatus.Cancelled)
                .ToListAsync();

        foreach (var task in tasks.Where(task =>
                     task.Deadline.HasValue
                     && task.Deadline.Value.Date < today))
        {
            model.CriticalNotifications.Add(new NotificationItemViewModel
            {
                Key = $"task-overdue-{task.Id}",
                Title = "Прострочене завдання",
                Message = $"Завдання «{task.Title}» прострочене. Дедлайн був {task.Deadline:dd.MM.yyyy}.",
                Severity = "critical",
                Category = "Завдання",
                EventDateTime = task.Deadline,
                ControllerName = "TheatreTasks",
                ActionName = "Details",
                EntityId = task.Id
            });
        }

        foreach (var task in tasks.Where(task =>
                     task.Priority == TheatreTaskPriority.Critical))
        {
            model.CriticalNotifications.Add(new NotificationItemViewModel
            {
                Key = $"task-critical-{task.Id}",
                Title = "Критичне завдання",
                Message = $"Завдання «{task.Title}» має критичний пріоритет.",
                Severity = "critical",
                Category = "Завдання",
                EventDateTime = task.Deadline,
                ControllerName = "TheatreTasks",
                ActionName = "Details",
                EntityId = task.Id
            });
        }

        foreach (var task in tasks.Where(task =>
                     task.Deadline.HasValue
                     && task.Deadline.Value.Date == today))
        {
            model.WarningNotifications.Add(new NotificationItemViewModel
            {
                Key = $"task-due-today-{task.Id}",
                Title = "Дедлайн сьогодні",
                Message = $"Завдання «{task.Title}» має дедлайн сьогодні.",
                Severity = "warning",
                Category = "Завдання",
                EventDateTime = task.Deadline,
                ControllerName = "TheatreTasks",
                ActionName = "Details",
                EntityId = task.Id
            });
        }
    }

    private async Task AddRehearsalNotificationsAsync(
        NotificationsIndexViewModel model,
        DateTime today,
        DateTime tomorrow,
        DateTime afterTomorrow,
        ApplicationUser currentUser,
        ClaimsPrincipal user)
    {
        var query =
            _context.Rehearsals
                .Include(rehearsal => rehearsal.Performance)
                .Include(rehearsal => rehearsal.Hall)
                    .ThenInclude(hall => hall.Venue)
                .Include(rehearsal => rehearsal.Participants)
                .Where(rehearsal => rehearsal.StartDateTime >= today
                                    && rehearsal.StartDateTime < afterTomorrow);

        if (user.IsInRole(UserRoles.Actor) && currentUser.ActorId != null)
        {
            var actorId = currentUser.ActorId.Value;

            query =
                query.Where(rehearsal =>
                    rehearsal.Participants.Any(participant =>
                        participant.ActorId == actorId));
        }

        var rehearsals =
            await query
                .OrderBy(rehearsal => rehearsal.StartDateTime)
                .ToListAsync();

        foreach (var rehearsal in rehearsals)
        {
            var isToday =
                rehearsal.StartDateTime.Date == today;

            var title =
                isToday
                    ? "Репетиція сьогодні"
                    : "Репетиція завтра";

            var dateText =
                isToday
                    ? "сьогодні"
                    : "завтра";

            var hallText =
                rehearsal.Hall != null
                    ? $"{rehearsal.Hall.Venue.Name} — {rehearsal.Hall.Name}"
                    : "зал не вказано";

            model.InfoNotifications.Add(new NotificationItemViewModel
            {
                Key = $"rehearsal-upcoming-{rehearsal.Id}",
                Title = title,
                Message = $"{dateText} о {rehearsal.StartDateTime:HH:mm} репетиція вистави «{rehearsal.Performance.Title}». Місце: {hallText}.",
                Severity = "info",
                Category = "Репетиції",
                EventDateTime = rehearsal.StartDateTime,
                ControllerName = "Rehearsals",
                ActionName = "Details",
                EntityId = rehearsal.Id
            });
        }
    }

    private async Task AddShowNotificationsAsync(
        NotificationsIndexViewModel model,
        DateTime today,
        DateTime tomorrow,
        DateTime afterTomorrow,
        ApplicationUser currentUser,
        ClaimsPrincipal user)
    {
        var query =
            _context.PerformanceShows
                .Include(show => show.Performance)
                .Include(show => show.Hall!)
                    .ThenInclude(hall => hall.Venue)
                .Where(show => show.StartDateTime >= today
                               && show.StartDateTime < afterTomorrow);

        if (user.IsInRole(UserRoles.Actor) && currentUser.ActorId != null)
        {
            var actorId = currentUser.ActorId.Value;

            query =
                query.Where(show =>
                    _context.RoleAssignments.Any(assignment =>
                        assignment.ActorId == actorId
                        && assignment.CharacterRole.PerformanceId == show.PerformanceId));
        }

        var shows =
            await query
                .OrderBy(show => show.StartDateTime)
                .ToListAsync();

        foreach (var show in shows)
        {
            var isToday =
                show.StartDateTime.Date == today;

            var title =
                isToday
                    ? "Показ сьогодні"
                    : "Показ завтра";

            var dateText =
                isToday
                    ? "сьогодні"
                    : "завтра";

            var placeText =
                show.Hall != null
                    ? $"{show.Hall.Venue.Name} — {show.Hall.Name}"
                    : show.ExternalLocation ?? "місце не вказано";

            model.InfoNotifications.Add(new NotificationItemViewModel
            {
                Key = $"show-upcoming-{show.Id}",
                Title = title,
                Message = $"{dateText} о {show.StartDateTime:HH:mm} показ вистави «{show.Performance.Title}». Місце: {placeText}.",
                Severity = "info",
                Category = "Покази",
                EventDateTime = show.StartDateTime,
                ControllerName = "PerformanceShows",
                ActionName = "Details",
                EntityId = show.Id
            });
        }

        var unconfirmedShows =
            await _context.PerformanceShows
                .Include(show => show.Performance)
                .Where(show => show.StartDateTime >= today
                               && show.Status == PerformanceShowStatus.Planned)
                .OrderBy(show => show.StartDateTime)
                .Take(10)
                .ToListAsync();

        foreach (var show in unconfirmedShows)
        {
            model.WarningNotifications.Add(new NotificationItemViewModel
            {
                Key = $"show-unconfirmed-{show.Id}",
                Title = "Непідтверджений показ",
                Message = $"Показ вистави «{show.Performance.Title}» запланований на {show.StartDateTime:dd.MM.yyyy HH:mm}, але ще не підтверджений.",
                Severity = "warning",
                Category = "Покази",
                EventDateTime = show.StartDateTime,
                ControllerName = "PerformanceShows",
                ActionName = "Details",
                EntityId = show.Id
            });
        }
    }

    private async Task AddProductionItemNotificationsAsync(
        NotificationsIndexViewModel model)
    {
        var missingItems =
            await _context.ProductionItems
                .Include(item => item.Performance)
                .Where(item => item.Status == ProductionItemStatus.Missing)
                .OrderBy(item => item.Performance.Title)
                .ThenBy(item => item.Name)
                .Take(20)
                .ToListAsync();

        foreach (var item in missingItems)
        {
            model.CriticalNotifications.Add(new NotificationItemViewModel
            {
                Key = $"production-missing-{item.Id}",
                Title = "Відсутній постановочний елемент",
                Message = $"Для вистави «{item.Performance.Title}» відсутній елемент: «{item.Name}».",
                Severity = "critical",
                Category = "Постановка",
                ControllerName = "ProductionItems",
                ActionName = "Details",
                EntityId = item.Id
            });
        }

        var notReadyItems =
            await _context.ProductionItems
                .Include(item => item.Performance)
                .Where(item => item.Status == ProductionItemStatus.Needed
                               || item.Status == ProductionItemStatus.InProgress)
                .OrderBy(item => item.Performance.Title)
                .ThenBy(item => item.Name)
                .Take(20)
                .ToListAsync();

        foreach (var item in notReadyItems)
        {
            model.WarningNotifications.Add(new NotificationItemViewModel
            {
                Key = $"production-not-ready-{item.Id}",
                Title = "Елемент ще не готовий",
                Message = $"Для вистави «{item.Performance.Title}» елемент «{item.Name}» ще не готовий.",
                Severity = "warning",
                Category = "Постановка",
                ControllerName = "ProductionItems",
                ActionName = "Details",
                EntityId = item.Id
            });
        }
    }

    private async Task AddFinanceNotificationsAsync(
        NotificationsIndexViewModel model,
        DateTime today)
    {
        var plannedTransactions =
            await _context.BudgetTransactions
                .Include(transaction => transaction.Performance)
                .Where(transaction => transaction.Status == BudgetTransactionStatus.Planned
                                      && transaction.TransactionDate.Date <= today)
                .OrderBy(transaction => transaction.TransactionDate)
                .Take(20)
                .ToListAsync();

        foreach (var transaction in plannedTransactions)
        {
            model.WarningNotifications.Add(new NotificationItemViewModel
            {
                Key = $"finance-planned-{transaction.Id}",
                Title = "Фінансова операція ще запланована",
                Message = $"Операція «{transaction.Title}» по виставі «{transaction.Performance.Title}» має статус Planned.",
                Severity = "warning",
                Category = "Фінанси",
                EventDateTime = transaction.TransactionDate,
                ControllerName = "BudgetTransactions",
                ActionName = "Details",
                EntityId = transaction.Id
            });
        }

        var completedShowsWithoutIncome =
            await _context.PerformanceShows
                .Include(show => show.Performance)
                .Where(show => show.Status == PerformanceShowStatus.Completed
                               && !_context.BudgetTransactions.Any(transaction =>
                                   transaction.PerformanceShowId == show.Id
                                   && transaction.Type == BudgetTransactionType.Income
                                   && transaction.Status != BudgetTransactionStatus.Cancelled))
                .OrderByDescending(show => show.StartDateTime)
                .Take(20)
                .ToListAsync();

        foreach (var show in completedShowsWithoutIncome)
        {
            model.WarningNotifications.Add(new NotificationItemViewModel
            {
                Key = $"finance-show-no-income-{show.Id}",
                Title = "Показ без фінансового доходу",
                Message = $"Показ вистави «{show.Performance.Title}» завершено, але дохід для нього ще не внесено.",
                Severity = "warning",
                Category = "Фінанси",
                EventDateTime = show.StartDateTime,
                ControllerName = "PerformanceShows",
                ActionName = "Details",
                EntityId = show.Id
            });
        }
    }

    private async Task AddPremiereReadinessNotificationsAsync(
        NotificationsIndexViewModel model,
        DateTime today)
    {
        var deadlineDate =
            today.AddDays(14);

        var upcomingPremieres =
            await _context.PerformanceShows
                .Include(show => show.Performance)
                .Where(show => show.Type == PerformanceShowType.Premiere
                               && show.Status != PerformanceShowStatus.Cancelled
                               && show.StartDateTime >= today
                               && show.StartDateTime <= deadlineDate)
                .OrderBy(show => show.StartDateTime)
                .Take(10)
                .ToListAsync();

        foreach (var premiere in upcomingPremieres)
        {
            var performanceId =
                premiere.PerformanceId;

            var unfinishedTasksCount =
                await _context.TheatreTasks
                    .CountAsync(task =>
                        task.PerformanceId == performanceId
                        && task.Status != TheatreTaskStatus.Done
                        && task.Status != TheatreTaskStatus.Cancelled);

            var notReadyItemsCount =
                await _context.ProductionItems
                    .CountAsync(item =>
                        item.PerformanceId == performanceId
                        && item.Status != ProductionItemStatus.Ready
                        && item.Status != ProductionItemStatus.Cancelled);

            var unassignedRolesCount =
                await _context.CharacterRoles
                    .CountAsync(role =>
                        role.PerformanceId == performanceId
                        && !_context.RoleAssignments.Any(assignment =>
                            assignment.CharacterRoleId == role.Id
                            && assignment.Status == RoleAssignmentStatus.Approved));

            var hasStatusProblem =
                premiere.Performance.Status != PerformanceStatus.ReadyForPremiere
                && premiere.Performance.Status != PerformanceStatus.InRepertoire;

            var hasProblems =
                unfinishedTasksCount > 0
                || notReadyItemsCount > 0
                || unassignedRolesCount > 0
                || hasStatusProblem;

            if (!hasProblems)
            {
                continue;
            }

            var daysLeft =
                (premiere.StartDateTime.Date - today).Days;

            var severityIsCritical =
                daysLeft <= 3;

            var problems =
                new List<string>();

            if (unfinishedTasksCount > 0)
            {
                problems.Add($"невиконаних завдань: {unfinishedTasksCount}");
            }

            if (notReadyItemsCount > 0)
            {
                problems.Add($"неготових елементів постановки: {notReadyItemsCount}");
            }

            if (unassignedRolesCount > 0)
            {
                problems.Add($"ролей без затвердженого актора: {unassignedRolesCount}");
            }

            if (hasStatusProblem)
            {
                problems.Add("статус вистави ще не ReadyForPremiere / InRepertoire");
            }

            var message =
                $"Прем’єра вистави «{premiere.Performance.Title}» через {daysLeft} дн. Є проблеми: {string.Join(", ", problems)}.";

            var notification =
                new NotificationItemViewModel
                {
                    Key = $"premiere-readiness-{premiere.Id}",
                    Title = "Готовність до прем’єри",
                    Message = message,
                    Severity = severityIsCritical ? "critical" : "warning",
                    Category = "Готовність вистави",
                    EventDateTime = premiere.StartDateTime,
                    ControllerName = "Performances",
                    ActionName = "Preparation",
                    EntityId = premiere.PerformanceId
                };

            if (severityIsCritical)
            {
                model.CriticalNotifications.Add(notification);
            }
            else
            {
                model.WarningNotifications.Add(notification);
            }
        }
    }

    private async Task ApplyReadStatesAsync(
        NotificationsIndexViewModel model,
        string userId,
        bool includeRead)
    {
        var allNotifications =
            model.CriticalNotifications
                .Concat(model.WarningNotifications)
                .Concat(model.InfoNotifications)
                .ToList();

        var keys =
            allNotifications
                .Select(notification => notification.Key)
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Distinct()
                .ToList();

        if (!keys.Any())
        {
            return;
        }

        var readKeys =
            await _context.NotificationReadStates
                .Where(state => state.UserId == userId
                                && keys.Contains(state.NotificationKey))
                .Select(state => state.NotificationKey)
                .ToListAsync();

        foreach (var notification in allNotifications)
        {
            notification.IsRead =
                readKeys.Contains(notification.Key);
        }

        if (includeRead)
        {
            return;
        }

        model.CriticalNotifications =
            model.CriticalNotifications
                .Where(notification => !notification.IsRead)
                .ToList();

        model.WarningNotifications =
            model.WarningNotifications
                .Where(notification => !notification.IsRead)
                .ToList();

        model.InfoNotifications =
            model.InfoNotifications
                .Where(notification => !notification.IsRead)
                .ToList();
    }

    private static void SortNotifications(
        NotificationsIndexViewModel model)
    {
        model.CriticalNotifications =
            model.CriticalNotifications
                .OrderBy(notification => notification.EventDateTime ?? DateTime.MaxValue)
                .ThenBy(notification => notification.Title)
                .ToList();

        model.WarningNotifications =
            model.WarningNotifications
                .OrderBy(notification => notification.EventDateTime ?? DateTime.MaxValue)
                .ThenBy(notification => notification.Title)
                .ToList();

        model.InfoNotifications =
            model.InfoNotifications
                .OrderBy(notification => notification.EventDateTime ?? DateTime.MaxValue)
                .ThenBy(notification => notification.Title)
                .ToList();
    }
}