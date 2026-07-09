namespace TheatreHub.ViewModels;

public class RehearsalAttendanceViewModel
{
    public int RehearsalId { get; set; }

    public string PerformanceTitle { get; set; } = string.Empty;

    public DateTime StartDateTime { get; set; }

    public DateTime EndDateTime { get; set; }

    public string HallName { get; set; } = string.Empty;

    public string VenueName { get; set; } = string.Empty;

    public List<AttendanceParticipantViewModel> Participants { get; set; }
        = [];
}