namespace TheatreHub.ViewModels.AdminUsers;

public class AdminUserListItemViewModel
{
    public string Id { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? JobTitle { get; set; }

    public bool IsActive { get; set; }

    public int? ActorId { get; set; }

    public string? ActorName { get; set; }

    public List<string> Roles { get; set; } = [];
}