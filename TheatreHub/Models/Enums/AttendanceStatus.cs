using System.ComponentModel.DataAnnotations;

namespace TheatreHub.Models.Enums;

public enum AttendanceStatus
{
    [Display(Name = "Не відмічено")]
    NotMarked = 0,

    [Display(Name = "Присутній")]
    Present = 1,

    [Display(Name = "Запізнився")]
    Late = 2,

    [Display(Name = "Відсутній")]
    Absent = 3,

    [Display(Name = "Відсутність погоджена")]
    Excused = 4
}