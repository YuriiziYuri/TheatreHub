using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using TheatreHub.Data;
using TheatreHub.Models;

namespace TheatreHub.Services.ActionLogs;

public class UserActionLogService : IUserActionLogService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserActionLogService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task LogAsync(
        ClaimsPrincipal user,
        string actionType,
        string entityType,
        int? entityId,
        string? entityTitle,
        string description)
    {
        var currentUser =
            await _userManager.GetUserAsync(user);

        var log = new UserActionLog
        {
            UserId = currentUser?.Id,
            UserFullName = currentUser?.FullName ?? user.Identity?.Name ?? "Невідомий користувач",
            ActionType = actionType,
            EntityType = entityType,
            EntityId = entityId,
            EntityTitle = entityTitle,
            Description = description,
            CreatedAt = DateTime.Now
        };

        _context.UserActionLogs.Add(log);

        await _context.SaveChangesAsync();
    }
}