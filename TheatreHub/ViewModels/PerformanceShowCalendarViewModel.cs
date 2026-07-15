using TheatreHub.Models;
using TheatreHub.Models.Enums;

namespace TheatreHub.ViewModels;

public class PerformanceShowCalendarViewModel
{
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int? PerformanceId { get; set; }

    public int? HallId { get; set; }

    public PerformanceShowType? Type { get; set; }

    public PerformanceShowStatus? Status { get; set; }

    public List<Performance> Performances { get; set; } = [];

    public List<Hall> Halls { get; set; } = [];

    public List<PerformanceShowCalendarDayViewModel> Days { get; set; } = [];
}

public class PerformanceShowCalendarDayViewModel
{
    public DateTime Date { get; set; }

    public List<PerformanceShowCalendarItemViewModel> Shows { get; set; } = [];
}

public class PerformanceShowCalendarItemViewModel
{
    public int Id { get; set; }

    public string PerformanceTitle { get; set; } = string.Empty;

    public string TypeText { get; set; } = string.Empty;

    public string StatusText { get; set; } = string.Empty;

    public string StatusBadgeClass { get; set; } = "text-bg-secondary";

    public string LocationText { get; set; } = string.Empty;

    public DateTime StartDateTime { get; set; }

    public DateTime EndDateTime { get; set; }

    public int? ExpectedAudienceCount { get; set; }

    public int? ActualAudienceCount { get; set; }
}