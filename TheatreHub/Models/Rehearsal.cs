using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using TheatreHub.Models.Enums;

namespace TheatreHub.Models;

public class Rehearsal : IValidatableObject
{
    public int Id { get; set; }

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Оберіть виставу")]
    [Display(Name = "Вистава")]
    public int PerformanceId { get; set; }

    [ValidateNever]
    public Performance Performance { get; set; } = null!;

    [Display(Name = "Дія")]
    public int? ActId { get; set; }

    [ValidateNever]
    public Act? Act { get; set; }

    [Display(Name = "Сцена")]
    public int? SceneId { get; set; }

    [ValidateNever]
    public Scene? Scene { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Оберіть зал")]
    [Display(Name = "Зал")]
    public int HallId { get; set; }

    [ValidateNever]
    public Hall Hall { get; set; } = null!;

    [Required(ErrorMessage = "Вкажіть дату та час початку")]
    [Display(Name = "Початок репетиції")]
    public DateTime StartDateTime { get; set; }

    [Required(ErrorMessage = "Вкажіть дату та час завершення")]
    [Display(Name = "Завершення репетиції")]
    public DateTime EndDateTime { get; set; }

    [StringLength(
        1000,
        ErrorMessage = "Примітки не можуть перевищувати 1000 символів")]
    [Display(Name = "Примітки")]
    public string? Notes { get; set; }

    [Display(Name = "Статус")]
    public RehearsalStatus Status { get; set; }
        = RehearsalStatus.Planned;

    [ValidateNever]
    public ICollection<RehearsalParticipant> Participants { get; set; }
        = new List<RehearsalParticipant>();

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)

    {
        if (EndDateTime <= StartDateTime)
        {
            yield return new ValidationResult(
                "Час завершення має бути пізніше часу початку.",
                new[] { nameof(EndDateTime) });
        }

        if (StartDateTime == default)
        {
            yield return new ValidationResult(
                "Вкажіть дату та час початку репетиції.",
                new[] { nameof(StartDateTime) });
        }

        if (EndDateTime == default)
        {
            yield return new ValidationResult(
                "Вкажіть дату та час завершення репетиції.",
                new[] { nameof(EndDateTime) });
        }
    }
}