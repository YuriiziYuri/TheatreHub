using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace TheatreHub.Models;

public class SceneRole
{
    [Display(Name = "Сцена")]
    public int SceneId { get; set; }

    [ValidateNever]
    public Scene Scene { get; set; } = null!;

    [Display(Name = "Роль")]
    public int CharacterRoleId { get; set; }

    [ValidateNever]
    public CharacterRole CharacterRole { get; set; } = null!;

    [Display(Name = "Обов’язкова участь")]
    public bool IsRequired { get; set; } = true;

    [StringLength(
        500,
        ErrorMessage = "Примітка не може перевищувати 500 символів")]
    [Display(Name = "Примітка")]
    public string? Notes { get; set; }
}