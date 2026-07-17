using System.ComponentModel.DataAnnotations;
using TheatreHub.Models;

namespace TheatreHub.ViewModels.AdminUsers;

public class AdminUserEditViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть повне ім’я.")]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть Email.")]
    [EmailAddress(ErrorMessage = "Вкажіть коректний Email.")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [StringLength(150)]
    public string? JobTitle { get; set; }

    public bool IsActive { get; set; } = true;

    public int? ActorId { get; set; }

    public List<string> SelectedRoles { get; set; } = [];

    public List<string> AvailableRoles { get; set; } = [];

    public List<Actor> Actors { get; set; } = [];
}