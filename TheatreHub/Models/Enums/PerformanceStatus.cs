using System.ComponentModel.DataAnnotations;

namespace TheatreHub.Models.Enums;

public enum PerformanceStatus
{
    [Display(Name = "Ідея")]
    Idea,

    [Display(Name = "Планування")]
    Planning,

    [Display(Name = "Кастинг")]
    Casting,

    [Display(Name = "Репетиційний процес")]
    Rehearsal,

    [Display(Name = "Готова до показу")]
    Ready,

    [Display(Name = "Активна")]
    Active,

    [Display(Name = "Завершена")]
    Completed,

    [Display(Name = "Скасована")]
    Cancelled
}