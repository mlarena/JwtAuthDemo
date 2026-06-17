using System.ComponentModel.DataAnnotations;

namespace JwtAuthDemo.ViewModels;

public class CreateRoleViewModel
{
    [Required(ErrorMessage = "Введите название роли")]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Description { get; set; }
}

public class ManagePermissionsViewModel
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public List<PermissionGroup> PermissionGroups { get; set; } = new();
    public List<int> SelectedPermissionIds { get; set; } = new();
}

public class PermissionGroup
{
    public string Resource { get; set; } = string.Empty;
    public List<PermissionItem> Permissions { get; set; } = new();
}

public class PermissionItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}
