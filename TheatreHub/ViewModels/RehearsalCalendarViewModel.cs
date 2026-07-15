using TheatreHub.Models;

namespace TheatreHub.ViewModels;

public class RehearsalCalendarViewModel
{
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int? PerformanceId { get; set; }

    public int? ActorId { get; set; }

    public int? HallId { get; set; }

    public List<Performance> Performances { get; set; } = [];

    public List<Actor> Actors { get; set; } = [];

    public List<Hall> Halls { get; set; } = [];

    public List<RehearsalCalendarDayViewModel> Days { get; set; } = [];
}

public class RehearsalCalendarDayViewModel
{
    public DateTime Date { get; set; }

    public List<RehearsalCalendarItemViewModel> Rehearsals { get; set; } = [];
}

public class RehearsalCalendarItemViewModel
{
    public int Id { get; set; }

    public string PerformanceTitle { get; set; } = string.Empty;

    public string TargetText { get; set; } = string.Empty;

    public string HallText { get; set; } = string.Empty;

    public DateTime StartDateTime { get; set; }

    public DateTime EndDateTime { get; set; }

    public string StatusText { get; set; } = string.Empty;

    public int ParticipantsCount { get; set; }

    public bool IsPerformanceShow { get; set; }

    public string EventTypeText { get; set; } = "Репетиція";

    public string EventBadgeClass { get; set; } = "text-bg-primary";

    public string DetailsController =>
        IsPerformanceShow ? "PerformanceShows" : "Rehearsals";
}