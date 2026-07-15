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

    [Range(1, int.MaxValue, ErrorMessage = "Оберіть зал")]
    [Display(Name = "Зал")]
    public int HallId { get; set; }

    [Required(ErrorMessage = "Вкажіть дату та час початку")]
    [Display(Name = "Початок репетиції")]
    public DateTime StartDateTime { get; set; }

    [Required(ErrorMessage = "Вкажіть дату та час завершення")]
    [Display(Name = "Завершення репетиції")]
    public DateTime EndDateTime { get; set; }

    [StringLength(1000)]
    [Display(Name = "Примітки")]
    public string? Notes { get; set; }

    [Display(Name = "Статус")]
    public RehearsalStatus Status { get; set; }
        = RehearsalStatus.Planned;

    public List<Performance> Performances { get; set; } = [];

    public List<Hall> Halls { get; set; } = [];

    public List<ActorSelectionViewModel> Actors { get; set; } = [];

    [Display(Name = "Дія")]
    public int? ActId { get; set; }

    [Display(Name = "Сцена")]
    public int? SceneId { get; set; }

    public List<Act> Acts { get; set; } = [];

    public List<Scene> Scenes { get; set; } = [];
}