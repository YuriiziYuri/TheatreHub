using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace TheatreHub.Models;

public class CharacterRole
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Вкажіть ім’я персонажа")]
    [StringLength(100)]
    [Display(Name = "Персонаж")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Опис персонажа")]
    public string? Description { get; set; }

    [Display(Name = "Головна роль")]
    public bool IsMainRole { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Оберіть виставу")]
    [Display(Name = "Вистава")]
    public int PerformanceId { get; set; }

    [ValidateNever]
    public Performance Performance { get; set; } = null!;

    public ICollection<RoleAssignment> Assignments { get; set; }
    = new List<RoleAssignment>();
}