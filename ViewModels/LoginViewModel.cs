using System.ComponentModel.DataAnnotations;

namespace JwtAuthDemo.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Введите имя пользователя или email")]
    public string UserNameOrEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите пароль")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
