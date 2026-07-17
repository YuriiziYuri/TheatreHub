using TheatreHub.Models;

namespace TheatreHub.ViewModels.ActionLogs;

public class UserActionLogIndexViewModel
{
    public List<UserActionLog> Logs { get; set; } = [];

    public string? Search { get; set; }

    public string? ActionType { get; set; }

    public string? EntityType { get; set; }

    public DateTime? DateFrom { get; set; }

    public DateTime? DateTo { get; set; }

    public List<string> ActionTypes { get; set; } = [];

    public List<string> EntityTypes { get; set; } = [];
}