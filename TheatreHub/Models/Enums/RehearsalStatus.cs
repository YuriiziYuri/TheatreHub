using System.ComponentModel.DataAnnotations;

namespace TheatreHub.Models.Enums;

public enum RehearsalStatus
{
    [Display(Name = "Запланована")]
    Planned,

    [Display(Name = "Триває")]
    InProgress,

    [Display(Name = "Завершена")]
    Completed,

    [Display(Name = "Скасована")]
    Cancelled
}