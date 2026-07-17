using TheatreHub.Models;
using TheatreHub.Models.Enums;

namespace TheatreHub.ViewModels;

public class BudgetTransactionIndexViewModel
{
    public List<BudgetTransaction> Transactions { get; set; } = [];

    public List<Performance> Performances { get; set; } = [];

    public List<PerformanceShow> PerformanceShows { get; set; } = [];

    public List<ProductionItem> ProductionItems { get; set; } = [];

    public int? PerformanceShowId { get; set; }

    public int? ProductionItemId { get; set; }

    public int? PerformanceId { get; set; }

    public BudgetTransactionType? Type { get; set; }

    public BudgetTransactionCategory? Category { get; set; }

    public BudgetTransactionStatus? Status { get; set; }

    public string? Currency { get; set; }

    public DateTime? DateFrom { get; set; }

    public DateTime? DateTo { get; set; }

    public decimal PlannedIncomeTotal { get; set; }

    public decimal PlannedExpenseTotal { get; set; }

    public decimal PlannedProfit { get; set; }

    public decimal ActualIncomeTotal { get; set; }

    public decimal ActualExpenseTotal { get; set; }

    public decimal ActualProfit { get; set; }

}

public class BudgetTransactionFormViewModel
{
    public BudgetTransaction Transaction { get; set; } = new();

    public List<Performance> Performances { get; set; } = [];

    public List<PerformanceShow> PerformanceShows { get; set; } = [];

    public List<ProductionItem> ProductionItems { get; set; } = [];
}

public class PerformanceBudgetViewModel
{
    public Performance Performance { get; set; } = null!;

    public List<BudgetTransaction> Transactions { get; set; } = [];

    public List<CategoryBudgetSummaryViewModel> IncomeByCategory { get; set; } = [];

    public List<CategoryBudgetSummaryViewModel> ExpenseByCategory { get; set; } = [];

    public List<ShowProfitSummaryViewModel> ShowProfits { get; set; } = [];

    public DateTime? DateFrom { get; set; }

    public DateTime? DateTo { get; set; }

    public string? Currency { get; set; }

    public decimal PlannedBudget { get; set; }

    public decimal PlannedIncomeTotal { get; set; }

    public decimal PlannedExpenseTotal { get; set; }

    public decimal PlannedProfit { get; set; }

    public decimal ActualIncomeTotal { get; set; }

    public decimal ActualExpenseTotal { get; set; }

    public decimal ActualProfit { get; set; }

    public decimal PlannedBudgetRemaining { get; set; }

    public decimal PlannedBudgetUsagePercent { get; set; }
}

public class CategoryBudgetSummaryViewModel
{
    public BudgetTransactionCategory Category { get; set; }

    public decimal PlannedTotal { get; set; }

    public decimal ActualTotal { get; set; }
}

public class ShowProfitSummaryViewModel
{
    public int PerformanceShowId { get; set; }

    public DateTime StartDateTime { get; set; }

    public string LocationText { get; set; } = string.Empty;

    public decimal PlannedIncome { get; set; }

    public decimal PlannedExpense { get; set; }

    public decimal PlannedProfit { get; set; }

    public decimal ActualIncome { get; set; }

    public decimal ActualExpense { get; set; }

    public decimal ActualProfit { get; set; }
}