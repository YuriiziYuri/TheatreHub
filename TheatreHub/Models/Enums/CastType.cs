using System.ComponentModel.DataAnnotations;

namespace TheatreHub.Models.Enums;

public enum CastType
{
    [Display(Name = "Основний склад")]
    Main = 0,

    [Display(Name = "Запасний склад")]
    Reserve = 1
}