using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheatreHub.Data;
using TheatreHub.Models;
using TheatreHub.Models.Enums;
using TheatreHub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using TheatreHub.Constants;
using TheatreHub.Services.ActionLogs;

namespace TheatreHub.Controllers;

[Authorize]
public class PerformancesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IUserActionLogService _actionLogService;

    public PerformancesController(
        ApplicationDbContext context,
        IUserActionLogService actionLogService)
    {
        _context = context;
        _actionLogService = actionLogService;
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

        var budgetTransactions = await _context.BudgetTransactions
    .AsNoTracking()
    .Where(transaction =>
        transaction.PerformanceId == performance.Id &&
        transaction.Status != BudgetTransactionStatus.Cancelled)
    .ToListAsync();

        ViewBag.PlannedBudget =
            performance.PlannedBudget ?? 0;

        ViewBag.PlannedIncomeTotal =
            budgetTransactions
                .Where(transaction =>
                    transaction.Type == BudgetTransactionType.Income)
                .Sum(transaction =>
                    transaction.PlannedAmount);

        ViewBag.PlannedExpenseTotal =
            budgetTransactions
                .Where(transaction =>
                    transaction.Type == BudgetTransactionType.Expense)
                .Sum(transaction =>
                    transaction.PlannedAmount);

        ViewBag.ActualIncomeTotal =
            budgetTransactions
                .Where(transaction =>
                    transaction.Type == BudgetTransactionType.Income)
                .Sum(transaction =>
                    transaction.ActualAmount ?? 0);

        ViewBag.ActualExpenseTotal =
            budgetTransactions
                .Where(transaction =>
                    transaction.Type == BudgetTransactionType.Expense)
                .Sum(transaction =>
                    transaction.ActualAmount ?? 0);

        ViewBag.ActualProfit =
            ViewBag.ActualIncomeTotal - ViewBag.ActualExpenseTotal;

        ViewBag.PlannedBudgetRemaining =
            ViewBag.PlannedBudget - ViewBag.PlannedExpenseTotal;

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
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
    public IActionResult Create()
    {
        return View(new Performance());
    }

    // POST: Performances/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
    public async Task<IActionResult> Create(
        [Bind(
            "Title,Description,Genre,PremiereDate," +
            "DurationMinutes,Status,PlannedBudget")]
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
        await _actionLogService.LogAsync(
            User,
            "Create",
            "Performance",
            performance.Id,
            performance.Title,
            $"Створено виставу «{performance.Title}».");

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
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
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
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
    public async Task<IActionResult> Edit(
        int id,
        [Bind(
            "Id,Title,Description,Genre,PremiereDate," +
            "DurationMinutes,Status,PlannedBudget")]
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

        existingPerformance.PlannedBudget =
            performance.PlannedBudget;

        try
        {
            await _context.SaveChangesAsync();
            await _actionLogService.LogAsync(
                User,
                "Edit",
                "Performance",
                performance.Id,
                performance.Title,
                $"Відредаговано виставу «{performance.Title}».");
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
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
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

        ViewBag.BudgetTransactionsCount = await _context.BudgetTransactions
    .AsNoTracking()
    .CountAsync(transaction =>
        transaction.PerformanceId == performance.Id);

        return View(performance);
    }

    [Authorize(Policy = AppPolicies.CanViewFinance)]
    public async Task<IActionResult> Budget(
    int? id,
    DateTime? dateFrom,
    DateTime? dateTo,
    string? currency)
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

        var query = _context.BudgetTransactions
            .AsNoTracking()
            .Include(transaction => transaction.Performance)
            .Include(transaction => transaction.PerformanceShow)
                .ThenInclude(show => show!.Hall)
                    .ThenInclude(hall => hall!.Venue!)
            .Include(transaction => transaction.ProductionItem)
            .Where(transaction =>
                transaction.PerformanceId == performance.Id);

        if (dateFrom.HasValue)
        {
            query = query.Where(transaction =>
                transaction.TransactionDate.Date >= dateFrom.Value.Date);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(transaction =>
                transaction.TransactionDate.Date <= dateTo.Value.Date);
        }

        if (!string.IsNullOrWhiteSpace(currency))
        {
            query = query.Where(transaction =>
                transaction.Currency == currency.Trim().ToUpperInvariant());
        }

        var transactions = await query
            .OrderByDescending(transaction =>
                transaction.TransactionDate)
            .ThenByDescending(transaction =>
                transaction.Id)
            .ToListAsync();

        var activeTransactions = transactions
            .Where(transaction =>
                transaction.Status != BudgetTransactionStatus.Cancelled)
            .ToList();

        var plannedIncomeTotal = activeTransactions
            .Where(transaction =>
                transaction.Type == BudgetTransactionType.Income)
            .Sum(transaction =>
                transaction.PlannedAmount);

        var plannedExpenseTotal = activeTransactions
            .Where(transaction =>
                transaction.Type == BudgetTransactionType.Expense)
            .Sum(transaction =>
                transaction.PlannedAmount);

        var actualIncomeTotal = activeTransactions
            .Where(transaction =>
                transaction.Type == BudgetTransactionType.Income)
            .Sum(transaction =>
                transaction.ActualAmount ?? 0);

        var actualExpenseTotal = activeTransactions
            .Where(transaction =>
                transaction.Type == BudgetTransactionType.Expense)
            .Sum(transaction =>
                transaction.ActualAmount ?? 0);

        var plannedBudget =
            performance.PlannedBudget ?? 0;

        var plannedBudgetRemaining =
            plannedBudget - plannedExpenseTotal;

        var plannedBudgetUsagePercent =
            plannedBudget > 0
                ? Math.Round(plannedExpenseTotal / plannedBudget * 100, 2)
                : 0;

        var incomeByCategory = activeTransactions
            .Where(transaction =>
                transaction.Type == BudgetTransactionType.Income)
            .GroupBy(transaction =>
                transaction.Category)
            .Select(group =>
                new CategoryBudgetSummaryViewModel
                {
                    Category = group.Key,
                    PlannedTotal = group.Sum(transaction =>
                        transaction.PlannedAmount),
                    ActualTotal = group.Sum(transaction =>
                        transaction.ActualAmount ?? 0)
                })
            .OrderByDescending(item =>
                item.ActualTotal)
            .ThenByDescending(item =>
                item.PlannedTotal)
            .ToList();

        var expenseByCategory = activeTransactions
            .Where(transaction =>
                transaction.Type == BudgetTransactionType.Expense)
            .GroupBy(transaction =>
                transaction.Category)
            .Select(group =>
                new CategoryBudgetSummaryViewModel
                {
                    Category = group.Key,
                    PlannedTotal = group.Sum(transaction =>
                        transaction.PlannedAmount),
                    ActualTotal = group.Sum(transaction =>
                        transaction.ActualAmount ?? 0)
                })
            .OrderByDescending(item =>
                item.ActualTotal)
            .ThenByDescending(item =>
                item.PlannedTotal)
            .ToList();

        var showProfits = activeTransactions
            .Where(transaction =>
                transaction.PerformanceShow != null)
            .GroupBy(transaction =>
                transaction.PerformanceShow!)
            .Select(group =>
            {
                var plannedIncome = group
                    .Where(transaction =>
                        transaction.Type == BudgetTransactionType.Income)
                    .Sum(transaction =>
                        transaction.PlannedAmount);

                var plannedExpense = group
                    .Where(transaction =>
                        transaction.Type == BudgetTransactionType.Expense)
                    .Sum(transaction =>
                        transaction.PlannedAmount);

                var actualIncome = group
                    .Where(transaction =>
                        transaction.Type == BudgetTransactionType.Income)
                    .Sum(transaction =>
                        transaction.ActualAmount ?? 0);

                var actualExpense = group
                    .Where(transaction =>
                        transaction.Type == BudgetTransactionType.Expense)
                    .Sum(transaction =>
                        transaction.ActualAmount ?? 0);

                return new ShowProfitSummaryViewModel
                {
                    PerformanceShowId = group.Key.Id,
                    StartDateTime = group.Key.StartDateTime,
                    LocationText = GetBudgetShowLocationText(group.Key),
                    PlannedIncome = plannedIncome,
                    PlannedExpense = plannedExpense,
                    PlannedProfit = plannedIncome - plannedExpense,
                    ActualIncome = actualIncome,
                    ActualExpense = actualExpense,
                    ActualProfit = actualIncome - actualExpense
                };
            })
            .OrderBy(item =>
                item.StartDateTime)
            .ToList();

        var model = new PerformanceBudgetViewModel
        {
            Performance = performance,
            Transactions = transactions,

            DateFrom = dateFrom,
            DateTo = dateTo,
            Currency = string.IsNullOrWhiteSpace(currency)
                ? null
                : currency.Trim().ToUpperInvariant(),

            PlannedBudget = plannedBudget,

            PlannedIncomeTotal = plannedIncomeTotal,
            PlannedExpenseTotal = plannedExpenseTotal,
            PlannedProfit = plannedIncomeTotal - plannedExpenseTotal,

            ActualIncomeTotal = actualIncomeTotal,
            ActualExpenseTotal = actualExpenseTotal,
            ActualProfit = actualIncomeTotal - actualExpenseTotal,

            PlannedBudgetRemaining = plannedBudgetRemaining,
            PlannedBudgetUsagePercent = plannedBudgetUsagePercent,

            IncomeByCategory = incomeByCategory,
            ExpenseByCategory = expenseByCategory,
            ShowProfits = showProfits
        };

        return View(model);
    }

    // POST: Performances/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManagePerformances)]
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

        var hasBudgetTransactions = await _context.BudgetTransactions
            .AsNoTracking()
            .AnyAsync(transaction =>
                transaction.PerformanceId == id);

        if (hasBudgetTransactions)
        {
            TempData["ErrorMessage"] =
                "Неможливо видалити виставу, тому що до неї прив’язані фінансові операції.";

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
        await _actionLogService.LogAsync(
                User,
                "Delete",
                "Performance",
                id,
                performanceTitle,
                $"Видалено виставу «{performanceTitle}».");

        TempData["SuccessMessage"] =
            $"Виставу «{performanceTitle}» видалено.";

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = AppPolicies.CanManagePerformances)]
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

    private static string GetBudgetShowLocationText(
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
}