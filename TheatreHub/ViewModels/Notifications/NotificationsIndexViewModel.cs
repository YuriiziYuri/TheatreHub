namespace TheatreHub.ViewModels.Notifications;

public class NotificationsIndexViewModel
{
    public List<NotificationItemViewModel> CriticalNotifications { get; set; } = [];

    public List<NotificationItemViewModel> WarningNotifications { get; set; } = [];

    public List<NotificationItemViewModel> InfoNotifications { get; set; } = [];

    public int TotalCount =>
        CriticalNotifications.Count
        + WarningNotifications.Count
        + InfoNotifications.Count;
}