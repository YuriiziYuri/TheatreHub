namespace TheatreHub.ViewModels;

public class DashboardViewModel
{
    public int PerformancesCount { get; set; }

    public int PerformancesInPreparationCount { get; set; }

    public int ActiveActorsCount { get; set; }

    public int UpcomingRehearsalsCount { get; set; }

    public int OpenTasksCount { get; set; }

    public int OverdueTasksCount { get; set; }

    public int CriticalTasksCount { get; set; }

    public int ProblemProductionItemsCount { get; set; }
    public decimal PlannedIncomeTotal { get; set; }

    public decimal PlannedExpenseTotal { get; set; }

    public decimal PlannedProfit { get; set; }

    public decimal ActualIncomeTotal { get; set; }

    public decimal ActualExpenseTotal { get; set; }

    public decimal ActualProfit { get; set; }

    public List<DashboardRehearsalItemViewModel> UpcomingRehearsals { get; set; } = [];

    public List<DashboardTaskItemViewModel> AttentionTasks { get; set; } = [];

    public List<DashboardProductionItemViewModel> ProblemProductionItems { get; set; } = [];

    public List<DashboardPerformanceItemViewModel> AttentionPerformances { get; set; } = [];

    public List<DashboardActionLogItemViewModel> RecentActionLogs { get; set; } = [];
}

public class DashboardRehearsalItemViewModel
{
    public int Id { get; set; }

    public DateTime StartDateTime { get; set; }

    public DateTime EndDateTime { get; set; }

    public string PerformanceTitle { get; set; } = string.Empty;

    public string TargetText { get; set; } = string.Empty;

    public string HallText { get; set; } = string.Empty;

    public string StatusText { get; set; } = string.Empty;
}

public class DashboardTaskItemViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string PerformanceTitle { get; set; } = string.Empty;

    public string TargetText { get; set; } = string.Empty;

    public string PriorityText { get; set; } = string.Empty;

    public DateTime? Deadline { get; set; }

    public bool IsOverdue { get; set; }
}

public class DashboardProductionItemViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string PerformanceTitle { get; set; } = string.Empty;

    public string TypeText { get; set; } = string.Empty;

    public string StatusText { get; set; } = string.Empty;

    public DateTime? NeededBy { get; set; }

    public bool IsOverdue { get; set; }
}

public class DashboardPerformanceItemViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string StatusText { get; set; } = string.Empty;

    public DateTime? PremiereDate { get; set; }

    public int RolesWithoutMainCast { get; set; }

    public int OpenTasksCount { get; set; }

    public int ProblemProductionItemsCount { get; set; }

    public string? NextRehearsalText { get; set; }

    public bool NeedsAttention =>
        RolesWithoutMainCast > 0 ||
        OpenTasksCount > 0 ||
        ProblemProductionItemsCount > 0;
}

public class DashboardActionLogItemViewModel
{
    public DateTime CreatedAt { get; set; }

    public string UserFullName { get; set; } = string.Empty;

    public string ActionType { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string? EntityTitle { get; set; }

    public string Description { get; set; } = string.Empty;
}