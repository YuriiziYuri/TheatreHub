using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace TheatreHub.Models;

public class Hall
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Оберіть майданчик")]
    [Range(1, int.MaxValue, ErrorMessage = "Оберіть майданчик")]
    [Display(Name = "Майданчик")]
    public int VenueId { get; set; }

    [ValidateNever]
    public Venue Venue { get; set; } = null!;

    [Required(ErrorMessage = "Вкажіть назву залу")]
    [StringLength(
        150,
        ErrorMessage = "Назва залу не може перевищувати 150 символів")]
    [Display(Name = "Назва залу")]
    public string Name { get; set; } = string.Empty;

    [Range(
        1,
        100000,
        ErrorMessage = "Місткість повинна бути більшою за нуль")]
    [Display(Name = "Місткість")]
    public int Capacity { get; set; }

    [Range(
        0,
        100000000,
        ErrorMessage = "Вартість оренди не може бути від’ємною")]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Вартість оренди")]
    public decimal RentalCost { get; set; }

    [Display(Name = "Сцена")]
    public bool HasStage { get; set; }

    [Display(Name = "Куліси")]
    public bool HasCurtains { get; set; }

    [Display(Name = "Гримерки")]
    public bool HasDressingRooms { get; set; }

    [Display(Name = "Світлове обладнання")]
    public bool HasLighting { get; set; }

    [Display(Name = "Звукове обладнання")]
    public bool HasSound { get; set; }

    [Display(Name = "Мікрофони")]
    public bool HasMicrophones { get; set; }

    [Display(Name = "Проєктор")]
    public bool HasProjector { get; set; }

    [StringLength(1000)]
    [Display(Name = "Додаткове обладнання")]
    public string? EquipmentNotes { get; set; }

    [StringLength(1000)]
    [Display(Name = "Примітки")]
    public string? Notes { get; set; }

    [Display(Name = "Активний зал")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Повна назва")]
    public string DisplayName =>
        $"{Venue?.Name ?? "Майданчик"} — {Name}";

    public ICollection<Rehearsal> Rehearsals { get; set; }
        = new List<Rehearsal>();
}