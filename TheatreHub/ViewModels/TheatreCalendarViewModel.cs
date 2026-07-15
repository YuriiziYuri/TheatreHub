using TheatreHub.Models;

namespace TheatreHub.ViewModels;

public class TheatreCalendarViewModel
{
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int? PerformanceId { get; set; }

    public int? ActorId { get; set; }

    public int? HallId { get; set; }

    public string EventType { get; set; } = "all";

    public bool IncludeCompleted { get; set; }

    public bool IncludeCancelled { get; set; }

    public List<Performance> Performances { get; set; } = [];

    public List<Actor> Actors { get; set; } = [];

    public List<Hall> Halls { get; set; } = [];

    public List<TheatreCalendarDayViewModel> Days { get; set; } = [];

    public int TotalEvents { get; set; }

    public int RehearsalsCount { get; set; }

    public int ShowsCount { get; set; }

    public int TasksCount { get; set; }

    public int PremieresCount { get; set; }

    public int OverdueTasksCount { get; set; }
}

public class TheatreCalendarDayViewModel
{
    public DateTime Date { get; set; }

    public List<TheatreCalendarItemViewModel> Items { get; set; } = [];
}

public class TheatreCalendarItemViewModel
{
    public int Id { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string EventTypeText { get; set; } = string.Empty;

    public string BadgeClass { get; set; } = "text-bg-secondary";

    public string ControllerName { get; set; } = string.Empty;

    public string ActionName { get; set; } = "Details";

    public string Title { get; set; } = string.Empty;

    public string Subtitle { get; set; } = string.Empty;

    public string Details { get; set; } = string.Empty;

    public string LocationText { get; set; } = string.Empty;

    public string StatusText { get; set; } = string.Empty;

    public DateTime StartDateTime { get; set; }

    public DateTime? EndDateTime { get; set; }

    public bool IsAllDay { get; set; }

    public bool IsOverdue { get; set; }

    public int SortOrder { get; set; }
}