using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using TheatreHub.Models.Enums;

namespace TheatreHub.Models;

public class ProductionItem
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Вкажіть назву.")]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public ProductionItemType Type { get; set; } =
        ProductionItemType.Prop;

    public ProductionItemStatus Status { get; set; } =
        ProductionItemStatus.Needed;

    [Range(
        1,
        10000,
        ErrorMessage = "Кількість має бути більшою за 0.")]
    public int Quantity { get; set; } = 1;

    [StringLength(120)]
    public string? ResponsibleName { get; set; }

    [StringLength(200)]
    public string? StorageLocation { get; set; }

    public DateTime? NeededBy { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

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