using System.ComponentModel.DataAnnotations;
using TheatreHub.Models.Enums;

namespace TheatreHub.Models;

public class Rehearsal : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Оберіть виставу")]
    [Display(Name = "Вистава")]
    public int PerformanceId { get; set; }

    public Performance Performance { get; set; } = null!;

    [Required(ErrorMessage = "Вкажіть дату та час початку")]
    [Display(Name = "Початок репетиції")]
    public DateTime StartDateTime { get; set; }

    [Required(ErrorMessage = "Вкажіть дату та час завершення")]
    [Display(Name = "Завершення репетиції")]
    public DateTime EndDateTime { get; set; }

    [Required(ErrorMessage = "Вкажіть місце проведення")]
    [StringLength(
        150,
        ErrorMessage = "Назва місця не може перевищувати 150 символів")]
    [Display(Name = "Місце проведення")]
    public string Location { get; set; } = string.Empty;

    [StringLength(
        1000,
        ErrorMessage = "Примітки не можуть перевищувати 1000 символів")]
    [Display(Name = "Примітки")]
    public string? Notes { get; set; }

    [Display(Name = "Статус")]
    public RehearsalStatus Status { get; set; }
        = RehearsalStatus.Planned;

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
    }
}