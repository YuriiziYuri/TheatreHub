using System.ComponentModel.DataAnnotations;
using TheatreHub.Models.Enums;

namespace TheatreHub.Models;

public class Performance
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Вкажіть назву вистави")]
    [StringLength(100, ErrorMessage = "Назва не може перевищувати 100 символів")]
    [Display(Name = "Назва")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Опис")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Вкажіть жанр")]
    [StringLength(50)]
    [Display(Name = "Жанр")]
    public string Genre { get; set; } = string.Empty;

    [Display(Name = "Дата прем'єри")]
    [DataType(DataType.Date)]
    public DateTime? PremiereDate { get; set; }

    [Range(0, 100000000, ErrorMessage = "Плановий бюджет не може бути від’ємним.")]
    public decimal? PlannedBudget { get; set; }

    [Range(1, 600, ErrorMessage = "Тривалість повинна бути від 1 до 600 хвилин")]
    [Display(Name = "Тривалість, хв")]
    public int DurationMinutes { get; set; }

    [Display(Name = "Статус")]
    public PerformanceStatus Status { get; set; } = PerformanceStatus.Idea;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<CharacterRole> CharacterRoles { get; set; }
        = new List<CharacterRole>();
    public ICollection<Rehearsal> Rehearsals { get; set; }
    = new List<Rehearsal>();
    public ICollection<Act> Acts { get; set; }
    = new List<Act>();
}