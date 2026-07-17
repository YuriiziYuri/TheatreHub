namespace TheatreHub.ViewModels.Notifications;

public class NotificationItemViewModel
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Severity { get; set; } = "info";

    public string Category { get; set; } = string.Empty;

    public DateTime? EventDateTime { get; set; }

    public string? ControllerName { get; set; }

    public string? ActionName { get; set; }

    public int? EntityId { get; set; }
    public bool IsRead { get; set; }
}