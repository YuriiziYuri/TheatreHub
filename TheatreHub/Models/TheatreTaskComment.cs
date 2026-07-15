using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace TheatreHub.Models;

public class TheatreTaskComment
{
    public int Id { get; set; }

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Коментар має бути прив’язаний до завдання.")]
    public int TheatreTaskId { get; set; }

    [ValidateNever]
    public TheatreTask TheatreTask { get; set; } = null!;

    [StringLength(120)]
    public string? AuthorName { get; set; }

    [Required(ErrorMessage = "Введіть текст коментаря.")]
    [StringLength(
        2000,
        ErrorMessage = "Коментар не може бути довшим за 2000 символів.")]
    public string Text { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } =
        DateTime.Now;
}