using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using TheatreHub.Models.Enums;

namespace TheatreHub.Models;

public class RoleAssignment : IValidatableObject
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Оберіть роль")]
    [Display(Name = "Роль")]
    public int CharacterRoleId { get; set; }

    [ValidateNever]
    public CharacterRole CharacterRole { get; set; } = null!;

    [Range(1, int.MaxValue, ErrorMessage = "Оберіть актора")]
    [Display(Name = "Актор")]
    public int ActorId { get; set; }

    [ValidateNever]
    public Actor Actor { get; set; } = null!;

    [Display(Name = "Акторський склад")]
    public CastType CastType { get; set; } = CastType.Main;

    [Required(ErrorMessage = "Вкажіть дату початку виконання ролі")]
    [DataType(DataType.Date)]
    [Display(Name = "Дата початку")]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Display(Name = "Дата завершення")]
    public DateTime? EndDate { get; set; }

    [Display(Name = "Поточний виконавець")]
    public bool IsCurrent { get; set; } = true;

    [Display(Name = "Показувати глядачам")]
    public bool IsPublic { get; set; }

    [Display(Name = "Статус призначення")]
    public RoleAssignmentStatus Status { get; set; }
        = RoleAssignmentStatus.Proposed;

    [StringLength(
        1000,
        ErrorMessage = "Примітка не може перевищувати 1000 символів")]
    [Display(Name = "Примітка")]
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (EndDate.HasValue &&
            EndDate.Value.Date < StartDate.Date)
        {
            yield return new ValidationResult(
                "Дата завершення не може бути раніше дати початку.",
                new[] { nameof(EndDate) });
        }

        if (IsCurrent && EndDate.HasValue)
        {
            yield return new ValidationResult(
                "Поточний виконавець не повинен мати дату завершення.",
                new[] { nameof(EndDate), nameof(IsCurrent) });
        }

        if (!IsCurrent && !EndDate.HasValue)
        {
            yield return new ValidationResult(
                "Для завершеного призначення потрібно вказати дату завершення.",
                new[] { nameof(EndDate) });
        }
    }
}