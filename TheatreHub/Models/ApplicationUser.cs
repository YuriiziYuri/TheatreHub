using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace TheatreHub.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(150)]
    public string? JobTitle { get; set; }

    public bool IsActive { get; set; } = true;

    public int? ActorId { get; set; }

    public Actor? Actor { get; set; }
}