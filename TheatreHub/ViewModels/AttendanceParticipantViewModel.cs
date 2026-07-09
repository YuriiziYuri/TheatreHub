using System.ComponentModel.DataAnnotations;
using TheatreHub.Models.Enums;

namespace TheatreHub.ViewModels;

public class AttendanceParticipantViewModel
{
    public int ActorId { get; set; }

    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Підтвердження участі")]
    public ParticipationResponseStatus ResponseStatus { get; set; }

    [Display(Name = "Відвідування")]
    public AttendanceStatus AttendanceStatus { get; set; }

    [Range(
        0,
        600,
        ErrorMessage = "Запізнення має бути від 0 до 600 хвилин")]
    [Display(Name = "Запізнення, хв")]
    public int LateMinutes { get; set; }

    [StringLength(500)]
    [Display(Name = "Причина відсутності")]
    public string? AbsenceReason { get; set; }

    [StringLength(1000)]
    [Display(Name = "Коментар")]
    public string? Comment { get; set; }
}