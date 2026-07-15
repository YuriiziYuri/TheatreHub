using TheatreHub.Models.Enums;

namespace TheatreHub.ViewModels;

public class PerformancePreparationViewModel
{
    public int PerformanceId { get; set; }

    public string PerformanceTitle { get; set; } = string.Empty;

    public PerformanceStatus PerformanceStatus { get; set; }

    public DateTime? PremiereDate { get; set; }

    public int RolesTotal { get; set; }

    public int RolesWithMainCast { get; set; }

    public int RolesWithoutMainCast { get; set; }

    public int RolesWithoutReserveCast { get; set; }

    public List<PreparationIssueViewModel> RoleIssues { get; set; } = [];

    public int ScenesTotal { get; set; }

    public int ScenesWithRoles { get; set; }

    public int ScenesWithoutRoles { get; set; }

    public List<PreparationIssueViewModel> SceneIssues { get; set; } = [];

    public int RehearsalsTotal { get; set; }

    public int RehearsalsCompleted { get; set; }

    public int RehearsalsCancelled { get; set; }

    public string? NextRehearsalText { get; set; }

    public List<PreparationRehearsalItemViewModel> UpcomingRehearsals { get; set; } = [];

    public int TasksTotal { get; set; }

    public int TasksDone { get; set; }

    public int TasksInProgress { get; set; }

    public int TasksOverdue { get; set; }

    public int CriticalOpenTasks { get; set; }

    public List<PreparationTaskItemViewModel> ProblemTasks { get; set; } = [];

    public int ProductionItemsTotal { get; set; }

    public int ProductionItemsReady { get; set; }

    public int ProductionItemsInProgress { get; set; }

    public int ProductionItemsMissing { get; set; }

    public int ProductionItemsOverdue { get; set; }

    public List<PreparationProductionItemViewModel> ProblemProductionItems { get; set; } = [];

    public List<string> AttentionItems { get; set; } = [];

    public bool HasAttentionItems =>
        AttentionItems.Any();
}

public class PreparationIssueViewModel
{
    public string Title { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public string? Url { get; set; }
}

public class PreparationRehearsalItemViewModel
{
    public int Id { get; set; }

    public DateTime StartDateTime { get; set; }

    public DateTime EndDateTime { get; set; }

    public string TargetText { get; set; } = string.Empty;

    public string HallText { get; set; } = string.Empty;

    public string StatusText { get; set; } = string.Empty;
}

public class PreparationTaskItemViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string TargetText { get; set; } = string.Empty;

    public string StatusText { get; set; } = string.Empty;

    public string PriorityText { get; set; } = string.Empty;

    public DateTime? Deadline { get; set; }

    public bool IsOverdue { get; set; }
}

public class PreparationProductionItemViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string TypeText { get; set; } = string.Empty;

    public string TargetText { get; set; } = string.Empty;

    public string StatusText { get; set; } = string.Empty;

    public DateTime? NeededBy { get; set; }

    public bool IsOverdue { get; set; }
}