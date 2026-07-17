using System.ComponentModel.DataAnnotations;

namespace TheatreHub.ViewModels.AdminUsers;

public class AdminUserChangePasswordViewModel
{
    public string UserId { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть новий пароль.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль має містити мінімум 6 символів.")]
    public string NewPassword { get; set; } = string.Empty;
}