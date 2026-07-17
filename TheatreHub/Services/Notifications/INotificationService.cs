using System.Security.Claims;
using TheatreHub.ViewModels.Notifications;

namespace TheatreHub.Services.Notifications;

public interface INotificationService
{
    Task<NotificationsIndexViewModel> GetNotificationsAsync(
        ClaimsPrincipal user,
        bool includeRead = false);

    Task MarkAsReadAsync(
        ClaimsPrincipal user,
        string notificationKey);

    Task MarkAllAsReadAsync(
        ClaimsPrincipal user);
}