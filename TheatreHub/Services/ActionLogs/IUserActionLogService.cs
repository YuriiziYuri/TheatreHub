using System.Security.Claims;

namespace TheatreHub.Services.ActionLogs;

public interface IUserActionLogService
{
    Task LogAsync(
        ClaimsPrincipal user,
        string actionType,
        string entityType,
        int? entityId,
        string? entityTitle,
        string description);
}