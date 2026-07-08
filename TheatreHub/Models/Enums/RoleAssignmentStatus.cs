using System.ComponentModel.DataAnnotations;

namespace TheatreHub.Models.Enums;

public enum RoleAssignmentStatus
{
    [Display(Name = "Запропоновано")]
    Proposed = 0,

    [Display(Name = "Очікує затвердження")]
    PendingApproval = 1,

    [Display(Name = "Затверджено")]
    Approved = 2,

    [Display(Name = "Відхилено")]
    Rejected = 3,

    [Display(Name = "Завершено")]
    Ended = 4
}