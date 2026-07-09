using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using TheatreHub.Models.Enums;

namespace TheatreHub.Models;

public class RehearsalParticipant
{
    public int RehearsalId { get; set; }

    [ValidateNever]
    public Rehearsal Rehearsal { get; set; } = null!;

    public int ActorId { get; set; }

    [ValidateNever]
    public Actor Actor { get; set; } = null!;

    [Display(Name = "Підтвердження участі")]
    public ParticipationResponseStatus ResponseStatus { get; set; }
        = ParticipationResponseStatus.NotResponded;

    [Display(Name = "Відвідування")]
    public AttendanceStatus AttendanceStatus { get; set; }
        = AttendanceStatus.NotMarked;

    [Range(
        0,
        600,
        ErrorMessage = "Кількість хвилин запізнення має бути від 0 до 600")]
    [Display(Name = "Запізнення, хв")]
    public int LateMinutes { get; set; }

    [StringLength(
        500,
        ErrorMessage = "Причина відсутності не може перевищувати 500 символів")]
    [Display(Name = "Причина відсутності")]
    public string? AbsenceReason { get; set; }

    [StringLength(
        1000,
        ErrorMessage = "Коментар не може перевищувати 1000 символів")]
    [Display(Name = "Коментар")]
    public string? Comment { get; set; }
}