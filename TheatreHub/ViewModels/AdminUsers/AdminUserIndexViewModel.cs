namespace TheatreHub.ViewModels.AdminUsers;

public class AdminUserIndexViewModel
{
    public List<AdminUserListItemViewModel> Users { get; set; } = [];

    public string? SearchTerm { get; set; }

    public string? Role { get; set; }

    public bool? IsActive { get; set; }

    public List<string> AvailableRoles { get; set; } = [];
}