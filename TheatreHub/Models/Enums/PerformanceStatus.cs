using System.ComponentModel.DataAnnotations;

namespace TheatreHub.Models.Enums;

public enum PerformanceStatus
{
    [Display(Name = "Ідея")]
    Idea = 0,

    [Display(Name = "Планування")]
    Planning = 1,

    [Display(Name = "Кастинг")]
    Casting = 2,

    [Display(Name = "Репетиційний період")]
    RehearsalPeriod = 3,

    [Display(Name = "Технічна підготовка")]
    TechnicalPreparation = 4,

    [Display(Name = "Готова до прем’єри")]
    ReadyForPremiere = 5,

    [Display(Name = "У репертуарі")]
    InRepertoire = 6,

    [Display(Name = "Завершена")]
    Completed = 7,

    [Display(Name = "Скасована")]
    Cancelled = 8
}