using System.ComponentModel.DataAnnotations;

namespace TheatreHub.ViewModels;

public class SceneRoleSelectionViewModel
{
    public int CharacterRoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public bool IsMainRole { get; set; }

    public string AssignedActorsText { get; set; } = string.Empty;

    [Display(Name = "Бере участь")]
    public bool IsSelected { get; set; }

    [Display(Name = "Обов’язкова участь")]
    public bool IsRequired { get; set; } = true;

    [StringLength(500)]
    [Display(Name = "Примітка")]
    public string? Notes { get; set; }
}