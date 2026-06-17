using System.ComponentModel.DataAnnotations;

namespace JwtAuthDemo.ViewModels;

public class EditProfileViewModel
{
    public int Id { get; set; }

    [Required]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите email")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? FirstName { get; set; }

    [MaxLength(50)]
    public string? LastName { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Введите текущий пароль")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите новый пароль")]
    [DataType(DataType.Password)]
    [MinLength(8, ErrorMessage = "Минимальная длина пароля - 8 символов")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Подтвердите пароль")]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Пароли не совпадают")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
