using System.ComponentModel.DataAnnotations;

namespace TheatreHub.Models;

public class Actor
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Вкажіть ім’я")]
    [StringLength(50)]
    [Display(Name = "Ім’я")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть прізвище")]
    [StringLength(50)]
    [Display(Name = "Прізвище")]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Вкажіть коректну електронну адресу")]
    [StringLength(100)]
    [Display(Name = "Електронна пошта")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Вкажіть коректний номер телефону")]
    [StringLength(30)]
    [Display(Name = "Телефон")]
    public string? PhoneNumber { get; set; }

    [StringLength(1000)]
    [Display(Name = "Примітки")]
    public string? Notes { get; set; }

    [Display(Name = "Повне ім’я")]
    public string FullName => $"{FirstName} {LastName}";

    public ICollection<CharacterRole> CharacterRoles { get; set; }
        = new List<CharacterRole>();

    public ICollection<RehearsalParticipant> RehearsalParticipants { get; set; }
    = new List<RehearsalParticipant>();
}