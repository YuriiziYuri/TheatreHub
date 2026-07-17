using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheatreHub.Data;
using TheatreHub.Models;
using TheatreHub.Models.Enums;
using TheatreHub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using TheatreHub.Constants;
using TheatreHub.Services.ActionLogs;
using ClosedXML.Excel;

namespace TheatreHub.Controllers;

[Authorize(Policy = AppPolicies.CanViewFinance)]
public class BudgetTransactionsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IUserActionLogService _actionLogService;

    public BudgetTransactionsController(
        ApplicationDbContext context,
        IUserActionLogService actionLogService)
    {
        _context = context;
        _actionLogService = actionLogService;
    }

    private IQueryable<BudgetTransaction> ApplyFilters(
    IQueryable<BudgetTransaction> query,
    int? performanceId,
    int? performanceShowId,
    int? productionItemId,
    BudgetTransactionType? type,
    BudgetTransactionCategory? category,
    BudgetTransactionStatus? status,
    string? currency,
    DateTime? dateFrom,
    DateTime? dateTo)
    {
        if (performanceId.HasValue)
        {
            query = query.Where(transaction =>
                transaction.PerformanceId == performanceId.Value);
        }

        if (performanceShowId.HasValue)
        {
            query = query.Where(transaction =>
                transaction.PerformanceShowId == performanceShowId.Value);
        }

        if (productionItemId.HasValue)
        {
            query = query.Where(transaction =>
                transaction.ProductionItemId == productionItemId.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(transaction =>
                transaction.Type == type.Value);
        }

        if (category.HasValue)
        {
            query = query.Where(transaction =>
                transaction.Category == category.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(transaction =>
                transaction.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(currency))
        {
            var normalizedCurrency =
                currency.Trim().ToUpperInvariant();

            query = query.Where(transaction =>
                transaction.Currency == normalizedCurrency);
        }

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

        return query;
    }

    public async Task<IActionResult> Index(
     int? performanceId,
     int? performanceShowId,
     int? productionItemId,
     BudgetTransactionType? type,
     BudgetTransactionCategory? category,
     BudgetTransactionStatus? status,
     string? currency,
     DateTime? dateFrom,
     DateTime? dateTo)
    {
        var query = _context.BudgetTransactions
            .AsNoTracking()
            .Include(transaction => transaction.Performance)
            .Include(transaction => transaction.PerformanceShow)
            .Include(transaction => transaction.ProductionItem)
            .AsQueryable();

        query = ApplyFilters(
            query,
            performanceId,
            performanceShowId,
            productionItemId,
            type,
            category,
            status,
            currency,
            dateFrom,
            dateTo);

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

        var model = new BudgetTransactionIndexViewModel
        {
            Transactions = transactions,

            Performances = await _context.Performances
           .AsNoTracking()
           .OrderBy(performance =>
               performance.Title)
           .ToListAsync(),

            PerformanceShows = await _context.PerformanceShows
           .AsNoTracking()
           .Include(show => show.Performance)
           .OrderByDescending(show =>
               show.StartDateTime)
           .ToListAsync(),

            ProductionItems = await _context.ProductionItems
           .AsNoTracking()
           .Include(item => item.Performance)
           .OrderBy(item =>
               item.Performance.Title)
           .ThenBy(item =>
               item.Name)
           .ToListAsync(),

            PerformanceId = performanceId,
            PerformanceShowId = performanceShowId,
            ProductionItemId = productionItemId,
            Type = type,
            Category = category,
            Status = status,
            Currency = currency,
            DateFrom = dateFrom,
            DateTo = dateTo,

            PlannedIncomeTotal = plannedIncomeTotal,
            PlannedExpenseTotal = plannedExpenseTotal,
            PlannedProfit = plannedIncomeTotal - plannedExpenseTotal,

            ActualIncomeTotal = actualIncomeTotal,
            ActualExpenseTotal = actualExpenseTotal,
            ActualProfit = actualIncomeTotal - actualExpenseTotal
        };

        return View(model);
    }

    public async Task<IActionResult> ExportToExcel(
    int? performanceId,
    int? performanceShowId,
    int? productionItemId,
    BudgetTransactionType? type,
    BudgetTransactionCategory? category,
    BudgetTransactionStatus? status,
    string? currency,
    DateTime? dateFrom,
    DateTime? dateTo)
    {
        var query = _context.BudgetTransactions
            .AsNoTracking()
            .Include(transaction => transaction.Performance)
            .Include(transaction => transaction.PerformanceShow)
            .Include(transaction => transaction.ProductionItem)
            .AsQueryable();

        query = ApplyFilters(
            query,
            performanceId,
            performanceShowId,
            productionItemId,
            type,
            category,
            status,
            currency,
            dateFrom,
            dateTo);

        var transactions = await query
            .OrderByDescending(transaction => transaction.TransactionDate)
            .ThenByDescending(transaction => transaction.Id)
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

        using var workbook = new XLWorkbook();

        var worksheet =
            workbook.Worksheets.Add("Фінанси");

        worksheet.Cell(1, 1).Value = "Фінансовий звіт TheatreHub";
        worksheet.Range(1, 1, 1, 10).Merge();
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;

        worksheet.Cell(3, 1).Value = "Планові доходи";
        worksheet.Cell(3, 2).Value = plannedIncomeTotal;

        worksheet.Cell(4, 1).Value = "Планові витрати";
        worksheet.Cell(4, 2).Value = plannedExpenseTotal;

        worksheet.Cell(5, 1).Value = "Плановий прибуток";
        worksheet.Cell(5, 2).Value = plannedIncomeTotal - plannedExpenseTotal;

        worksheet.Cell(3, 4).Value = "Фактичні доходи";
        worksheet.Cell(3, 5).Value = actualIncomeTotal;

        worksheet.Cell(4, 4).Value = "Фактичні витрати";
        worksheet.Cell(4, 5).Value = actualExpenseTotal;

        worksheet.Cell(5, 4).Value = "Фактичний прибуток";
        worksheet.Cell(5, 5).Value = actualIncomeTotal - actualExpenseTotal;

        var summaryRange = worksheet.Range(3, 1, 5, 5);
        summaryRange.Style.Font.Bold = true;

        var headerRow = 8;

        worksheet.Cell(headerRow, 1).Value = "Дата";
        worksheet.Cell(headerRow, 2).Value = "Вистава";
        worksheet.Cell(headerRow, 3).Value = "Назва операції";
        worksheet.Cell(headerRow, 4).Value = "Тип";
        worksheet.Cell(headerRow, 5).Value = "Категорія";
        worksheet.Cell(headerRow, 6).Value = "Статус";
        worksheet.Cell(headerRow, 7).Value = "Планова сума";
        worksheet.Cell(headerRow, 8).Value = "Фактична сума";
        worksheet.Cell(headerRow, 9).Value = "Валюта";
        worksheet.Cell(headerRow, 10).Value = "Відповідальний";

        var headerRange =
            worksheet.Range(headerRow, 1, headerRow, 10);

        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor =
            XLColor.LightGray;

        for (var i = 0; i < transactions.Count; i++)
        {
            var transaction = transactions[i];
            var row = headerRow + i + 1;

            worksheet.Cell(row, 1).Value =
                transaction.TransactionDate.ToString("dd.MM.yyyy");

            worksheet.Cell(row, 2).Value =
                transaction.Performance?.Title ?? "";

            worksheet.Cell(row, 3).Value =
                transaction.Title;

            worksheet.Cell(row, 4).Value =
                transaction.Type.ToString();

            worksheet.Cell(row, 5).Value =
                transaction.Category.ToString();

            worksheet.Cell(row, 6).Value =
                transaction.Status.ToString();

            worksheet.Cell(row, 7).Value =
                transaction.PlannedAmount;

            worksheet.Cell(row, 8).Value =
                transaction.ActualAmount ?? 0;

            worksheet.Cell(row, 9).Value =
                transaction.Currency;

            worksheet.Cell(row, 10).Value =
                transaction.ResponsibleName ?? "";
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();

        workbook.SaveAs(stream);

        var fileName =
            $"budget-transactions-{DateTime.Now:yyyyMMdd-HHmm}.xlsx";

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var transaction = await _context.BudgetTransactions
            .AsNoTracking()
            .Include(item => item.Performance)
            .Include(item => item.PerformanceShow)
                .ThenInclude(show => show!.Hall)
                    .ThenInclude(hall => hall!.Venue!)
            .Include(item => item.ProductionItem)
            .FirstOrDefaultAsync(item =>
                item.Id == id.Value);

        if (transaction == null)
        {
            return NotFound();
        }

        return View(transaction);
    }

    [Authorize(Policy = AppPolicies.CanManageFinance)]
    public async Task<IActionResult> Create(
        int? performanceId,
        int? performanceShowId,
        int? productionItemId,
        BudgetTransactionType? type)
    {
        var transaction = new BudgetTransaction
        {
            TransactionDate = DateTime.Today,
            Currency = "UAH",
            Status = BudgetTransactionStatus.Planned,
            Type = type ?? BudgetTransactionType.Expense
        };

        if (performanceShowId.HasValue)
        {
            var show = await _context.PerformanceShows
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.Id == performanceShowId.Value);

            if (show != null)
            {
                transaction.PerformanceShowId = show.Id;
                transaction.PerformanceId = show.PerformanceId;
            }
        }
        else if (productionItemId.HasValue)
        {
            var productionItem = await _context.ProductionItems
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.Id == productionItemId.Value);

            if (productionItem != null)
            {
                transaction.ProductionItemId = productionItem.Id;
                transaction.PerformanceId = productionItem.PerformanceId;
            }
        }
        else if (performanceId.HasValue)
        {
            transaction.PerformanceId = performanceId.Value;
        }

        var model = new BudgetTransactionFormViewModel
        {
            Transaction = transaction
        };

        await LoadFormDataAsync(model);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageFinance)]
    public async Task<IActionResult> Create(BudgetTransactionFormViewModel model)
    {
        NormalizeTransaction(model.Transaction);
        ValidateTransaction(model.Transaction);

        if (ModelState.IsValid)
        {
            _context.BudgetTransactions.Add(model.Transaction);
            await _context.SaveChangesAsync();

            await _actionLogService.LogAsync(
                User,
                "Create",
                "BudgetTransaction",
                model.Transaction.Id,
                model.Transaction.Title,
                $"Створено фінансову операцію «{model.Transaction.Title}» на суму {model.Transaction.PlannedAmount} {model.Transaction.Currency}.");

            TempData["SuccessMessage"] = "Фінансову операцію створено.";

            return RedirectToAction(nameof(Details), new { id = model.Transaction.Id });
        }

        await LoadFormDataAsync(model);

        return View(model);
    }

    [Authorize(Policy = AppPolicies.CanManageFinance)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var transaction = await _context.BudgetTransactions
            .FirstOrDefaultAsync(item =>
                item.Id == id.Value);

        if (transaction == null)
        {
            return NotFound();
        }

        var model = new BudgetTransactionFormViewModel
        {
            Transaction = transaction
        };

        await LoadFormDataAsync(model);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageFinance)]
    public async Task<IActionResult> Edit(
        int id,
        BudgetTransactionFormViewModel model)
    {
        if (id != model.Transaction.Id)
        {
            return NotFound();
        }

        NormalizeTransaction(model.Transaction);
        ValidateTransaction(model.Transaction);

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(model.Transaction);
                await _context.SaveChangesAsync();

                await _actionLogService.LogAsync(
                    User,
                    "Edit",
                    "BudgetTransaction",
                    model.Transaction.Id,
                    model.Transaction.Title,
                    $"Оновлено фінансову операцію «{model.Transaction.Title}».");

                TempData["SuccessMessage"] = "Фінансову операцію оновлено.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await BudgetTransactionExistsAsync(model.Transaction.Id))
                {
                    return NotFound();
                }

                throw;
            }

            return RedirectToAction(nameof(Details), new { id = model.Transaction.Id });
        }

        await LoadFormDataAsync(model);

        return View(model);
    }

    [Authorize(Policy = AppPolicies.CanManageFinance)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var transaction = await _context.BudgetTransactions
            .AsNoTracking()
            .Include(item => item.Performance)
            .Include(item => item.PerformanceShow)
            .Include(item => item.ProductionItem)
            .FirstOrDefaultAsync(item =>
                item.Id == id.Value);

        if (transaction == null)
        {
            return NotFound();
        }

        return View(transaction);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageFinance)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var transaction = await _context.BudgetTransactions
            .FirstOrDefaultAsync(item =>
                item.Id == id);

        if (transaction == null)
        {
            return NotFound();
        }

        var transactionTitle = transaction.Title;
        var plannedAmount = transaction.PlannedAmount;
        var currency = transaction.Currency;

        _context.BudgetTransactions.Remove(transaction);
        await _context.SaveChangesAsync();

        await _actionLogService.LogAsync(
            User,
            "Delete",
            "BudgetTransaction",
            id,
            transactionTitle,
            $"Видалено фінансову операцію «{transactionTitle}» на суму {plannedAmount} {currency}.");

        TempData["SuccessMessage"] = "Фінансову операцію видалено.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AppPolicies.CanManageFinance)]
    public async Task<IActionResult> ChangeStatus(
        int id,
        BudgetTransactionStatus status)
    {
        var transaction = await _context.BudgetTransactions
            .FirstOrDefaultAsync(item =>
                item.Id == id);

        if (transaction == null)
        {
            return NotFound();
        }

        var oldStatus = transaction.Status;

        transaction.Status = status;

        await _context.SaveChangesAsync();

        await _actionLogService.LogAsync(
            User,
            "ChangeStatus",
            "BudgetTransaction",
            transaction.Id,
            transaction.Title,
            $"Статус фінансової операції «{transaction.Title}» змінено з {oldStatus} на {status}.");

        TempData["SuccessMessage"] = "Статус фінансової операції оновлено.";

        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task LoadFormDataAsync(BudgetTransactionFormViewModel model)
    {
        model.Performances = await _context.Performances
            .AsNoTracking()
            .OrderBy(performance =>
                performance.Title)
            .ToListAsync();

        model.PerformanceShows = await _context.PerformanceShows
            .AsNoTracking()
            .Include(show => show.Performance)
            .OrderByDescending(show =>
                show.StartDateTime)
            .ToListAsync();

        model.ProductionItems = await _context.ProductionItems
            .AsNoTracking()
            .Include(item => item.Performance)
            .OrderBy(item =>
                item.Performance.Title)
            .ThenBy(item =>
                item.Name)
            .ToListAsync();
    }

    private void NormalizeTransaction(BudgetTransaction transaction)
    {
        transaction.Title =
            NormalizeOptionalText(transaction.Title) ?? string.Empty;

        transaction.Description =
            NormalizeOptionalText(transaction.Description);

        transaction.Currency =
            NormalizeOptionalText(transaction.Currency)?.ToUpperInvariant() ?? "UAH";

        transaction.ResponsibleName =
            NormalizeOptionalText(transaction.ResponsibleName);

        transaction.Notes =
            NormalizeOptionalText(transaction.Notes);

        if (transaction.PerformanceShowId == 0)
        {
            transaction.PerformanceShowId = null;
        }

        if (transaction.ProductionItemId == 0)
        {
            transaction.ProductionItemId = null;
        }

        if (transaction.IsAutoCalculated &&
            transaction.Type == BudgetTransactionType.Income &&
            transaction.AudienceCount.HasValue &&
            transaction.TicketPrice.HasValue)
        {
            transaction.PlannedAmount =
                transaction.AudienceCount.Value *
                transaction.TicketPrice.Value;
        }

        if (!transaction.IsAutoCalculated)
        {
            transaction.AudienceCount = null;
            transaction.TicketPrice = null;
        }
    }

    private void ValidateTransaction(BudgetTransaction transaction)
    {
        if (transaction.PerformanceShowId.HasValue)
        {
            var showExistsForPerformance = _context.PerformanceShows
                .AsNoTracking()
                .Any(show =>
                    show.Id == transaction.PerformanceShowId.Value &&
                    show.PerformanceId == transaction.PerformanceId);

            if (!showExistsForPerformance)
            {
                ModelState.AddModelError(
                    "Transaction.PerformanceShowId",
                    "Обраний показ не належить до цієї вистави.");
            }
        }

        if (transaction.ProductionItemId.HasValue)
        {
            var productionItemExistsForPerformance = _context.ProductionItems
                .AsNoTracking()
                .Any(item =>
                    item.Id == transaction.ProductionItemId.Value &&
                    item.PerformanceId == transaction.PerformanceId);

            if (!productionItemExistsForPerformance)
            {
                ModelState.AddModelError(
                    "Transaction.ProductionItemId",
                    "Обраний постановочний елемент не належить до цієї вистави.");
            }
        }

        if (transaction.Type == BudgetTransactionType.Income &&
            transaction.ProductionItemId.HasValue)
        {
            ModelState.AddModelError(
                "Transaction.ProductionItemId",
                "Дохід не потрібно прив’язувати до постановочного елемента.");
        }

        if (transaction.IsAutoCalculated)
        {
            if (transaction.Type != BudgetTransactionType.Income)
            {
                ModelState.AddModelError(
                    "Transaction.IsAutoCalculated",
                    "Автоматичний розрахунок доступний тільки для доходів.");
            }

            if (!transaction.AudienceCount.HasValue)
            {
                ModelState.AddModelError(
                    "Transaction.AudienceCount",
                    "Вкажіть кількість глядачів.");
            }

            if (!transaction.TicketPrice.HasValue)
            {
                ModelState.AddModelError(
                    "Transaction.TicketPrice",
                    "Вкажіть ціну квитка.");
            }
        }
    }

    private async Task<bool> BudgetTransactionExistsAsync(int id)
    {
        return await _context.BudgetTransactions
            .AnyAsync(item =>
                item.Id == id);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}