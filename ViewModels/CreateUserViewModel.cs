using System.ComponentModel.DataAnnotations;

namespace JwtAuthDemo.ViewModels;

public class CreateUserViewModel
{
    [Required(ErrorMessage = "Введите имя пользователя")]
    [MaxLength(50)]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите email")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите пароль")]
    [DataType(DataType.Password)]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Пароли не совпадают")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? FirstName { get; set; }

    [MaxLength(50)]
    public string? LastName { get; set; }

    public List<int> SelectedRoleIds { get; set; } = new();
}

public class AssignRolesViewModel
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public List<int> SelectedRoleIds { get; set; } = new();
}
