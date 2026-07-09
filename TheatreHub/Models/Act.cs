using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace TheatreHub.Models;

public class Act
{
    public int Id { get; set; }

    [Range(
        1,
        100,
        ErrorMessage = "Номер дії має бути від 1 до 100")]
    [Display(Name = "Номер дії")]
    public int Number { get; set; }

    [StringLength(
        150,
        ErrorMessage = "Назва не може перевищувати 150 символів")]
    [Display(Name = "Назва дії")]
    public string? Title { get; set; }

    [StringLength(
        2000,
        ErrorMessage = "Опис не може перевищувати 2000 символів")]
    [Display(Name = "Опис")]
    public string? Description { get; set; }

    [Range(
        1,
        1000,
        ErrorMessage = "Позиція має бути від 1 до 1000")]
    [Display(Name = "Порядок")]
    public int Position { get; set; }

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Оберіть виставу")]
    [Display(Name = "Вистава")]
    public int PerformanceId { get; set; }

    [ValidateNever]
    public Performance Performance { get; set; } = null!;

    public ICollection<Scene> Scenes { get; set; }
        = new List<Scene>();

    public string DisplayName =>
        string.IsNullOrWhiteSpace(Title)
            ? $"Дія {Number}"
            : $"Дія {Number}. {Title}";
}