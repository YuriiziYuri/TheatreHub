using System.ComponentModel.DataAnnotations;

namespace TheatreHub.Models.Enums;

public enum ParticipationResponseStatus
{
    [Display(Name = "Відповіді немає")]
    NotResponded = 0,

    [Display(Name = "Участь підтверджено")]
    Confirmed = 1,

    [Display(Name = "Не зможе бути присутнім")]
    Declined = 2
}