using System.ComponentModel.DataAnnotations;

namespace TheatreHub.Models;

public class NotificationReadState
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string NotificationKey { get; set; } = string.Empty;

    public DateTime ReadAt { get; set; } = DateTime.Now;
}