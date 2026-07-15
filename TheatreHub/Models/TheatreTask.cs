using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using TheatreHub.Models.Enums;

namespace TheatreHub.Models;

public class TheatreTask
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Вкажіть назву завдання.")]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(120)]
    public string? ResponsibleName { get; set; }

    public DateTime? Deadline { get; set; }

    public TheatreTaskStatus Status { get; set; } =
        TheatreTaskStatus.Planned;

    public TheatreTaskPriority Priority { get; set; } =
        TheatreTaskPriority.Normal;

    public DateTime CreatedAt { get; set; } =
        DateTime.Now;

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Оберіть виставу.")]
    public int PerformanceId { get; set; }

    [ValidateNever]
    public Performance Performance { get; set; } = null!;

    public int? ActId { get; set; }

    [ValidateNever]
    public Act? Act { get; set; }

    public int? SceneId { get; set; }

    [ValidateNever]
    public Scene? Scene { get; set; }
}