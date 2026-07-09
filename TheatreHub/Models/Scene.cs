using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace TheatreHub.Models;

public class Scene
{
    public int Id { get; set; }

    [Range(
        1,
        1000,
        ErrorMessage = "Номер сцени має бути від 1 до 1000")]
    [Display(Name = "Номер сцени")]
    public int Number { get; set; }

    [Required(ErrorMessage = "Вкажіть назву сцени")]
    [StringLength(
        150,
        ErrorMessage = "Назва не може перевищувати 150 символів")]
    [Display(Name = "Назва сцени")]
    public string Title { get; set; } = string.Empty;

    [StringLength(
        3000,
        ErrorMessage = "Опис не може перевищувати 3000 символів")]
    [Display(Name = "Короткий зміст")]
    public string? Synopsis { get; set; }

    [Range(
        0,
        1440,
        ErrorMessage = "Тривалість має бути від 0 до 1440 хвилин")]
    [Display(Name = "Тривалість, хв")]
    public int DurationMinutes { get; set; }

    [Range(
        1,
        1000,
        ErrorMessage = "Позиція має бути від 1 до 1000")]
    [Display(Name = "Порядок")]
    public int Position { get; set; }

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Оберіть дію")]
    [Display(Name = "Дія")]
    public int ActId { get; set; }

    [ValidateNever]
    public Act Act { get; set; } = null!;
}