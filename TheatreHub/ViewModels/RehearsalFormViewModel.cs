using System.ComponentModel.DataAnnotations;
using TheatreHub.Models;
using TheatreHub.Models.Enums;

namespace TheatreHub.ViewModels;

public class RehearsalFormViewModel
{
    public int? Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Оберіть виставу")]
    [Display(Name = "Вистава")]
    public int PerformanceId { get; set; }

    [Required(ErrorMessage = "Вкажіть дату та час початку")]
    [Display(Name = "Початок репетиції")]
    public DateTime StartDateTime { get; set; }

    [Required(ErrorMessage = "Вкажіть дату та час завершення")]
    [Display(Name = "Завершення репетиції")]
    public DateTime EndDateTime { get; set; }

    [Required(ErrorMessage = "Вкажіть місце проведення")]
    [StringLength(150)]
    [Display(Name = "Місце проведення")]
    public string Location { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Примітки")]
    public string? Notes { get; set; }

    [Display(Name = "Статус")]
    public RehearsalStatus Status { get; set; }
        = RehearsalStatus.Planned;

    public List<Performance> Performances { get; set; } = [];

    public List<ActorSelectionViewModel> Actors { get; set; } = [];
}