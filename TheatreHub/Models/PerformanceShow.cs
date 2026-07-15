using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using TheatreHub.Models.Enums;

namespace TheatreHub.Models;

public class PerformanceShow
{
    public int Id { get; set; }

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Оберіть виставу.")]
    public int PerformanceId { get; set; }

    [ValidateNever]
    public Performance Performance { get; set; } = null!;

    public int? HallId { get; set; }

    [ValidateNever]
    public Hall? Hall { get; set; }

    [StringLength(200)]
    public string? ExternalLocation { get; set; }

    [Required(ErrorMessage = "Вкажіть дату і час початку показу.")]
    public DateTime StartDateTime { get; set; }

    [Required(ErrorMessage = "Вкажіть дату і час завершення показу.")]
    public DateTime EndDateTime { get; set; }

    public PerformanceShowType Type { get; set; } =
        PerformanceShowType.Regular;

    public PerformanceShowStatus Status { get; set; } =
        PerformanceShowStatus.Planned;

    [Range(
        0,
        100000,
        ErrorMessage = "Кількість глядачів не може бути від’ємною.")]
    public int? ExpectedAudienceCount { get; set; }

    [Range(
        0,
        100000,
        ErrorMessage = "Кількість глядачів не може бути від’ємною.")]
    public int? ActualAudienceCount { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } =
        DateTime.Now;
}