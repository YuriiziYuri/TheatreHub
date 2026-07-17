using System.ComponentModel.DataAnnotations;

namespace TheatreHub.Models;

public class UserActionLog
{
    public int Id { get; set; }

    [StringLength(450)]
    public string? UserId { get; set; }

    [StringLength(200)]
    public string UserFullName { get; set; } = "Невідомий користувач";

    [StringLength(100)]
    public string ActionType { get; set; } = string.Empty;

    [StringLength(100)]
    public string EntityType { get; set; } = string.Empty;

    public int? EntityId { get; set; }

    [StringLength(300)]
    public string? EntityTitle { get; set; }

    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}