using JwtAuthDemo.Models.Entities;
using JwtAuthDemo.Services;
using JwtAuthDemo.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JwtAuthDemo.Controllers;

[Authorize]
public class RolesController : Controller
{
    private readonly IRoleService _roleService;
    private readonly IAuditLogService _auditLogService;

    public RolesController(IRoleService roleService, IAuditLogService auditLogService)
    {
        _roleService = roleService;
        _auditLogService = auditLogService;
    }

    [Authorize(Policy = "CanManageRoles")]
    public async Task<IActionResult> Index()
    {
        var roles = await _roleService.GetAllRolesAsync();
        var roleViewModels = new List<RoleViewModel>();

        foreach (var role in roles)
        {
            roleViewModels.Add(new RoleViewModel
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                IsSystem = role.IsSystem,
                UserCount = await _roleService.GetRoleUserCountAsync(role.Id),
                PermissionCount = role.RolePermissions.Count
            });
        }

        return View(roleViewModels);
    }

    [Authorize(Policy = "CanManageRoles")]
    [HttpGet]
    public IActionResult Create() => View();

    [Authorize(Policy = "CanManageRoles")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRoleViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var role = new Role { Name = model.Name, Description = model.Description };
        if (!await _roleService.CreateRoleAsync(role))
        {
            ModelState.AddModelError(string.Empty, "Роль с таким именем уже существует");
            return View(model);
        }

        await _auditLogService.LogAsync(GetCurrentUserId(), "Role Created", "roles", GetClientIp(), GetUserAgent(), details: $"Created role: {model.Name}");

        TempData["SuccessMessage"] = "Роль создана";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "CanManageRoles")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var role = await _roleService.GetRoleByIdAsync(id);
        if (role == null || role.IsSystem) return NotFound();

        var model = new CreateRoleViewModel { Name = role.Name, Description = role.Description };
        ViewBag.RoleId = role.Id;
        return View(model);
    }

    [Authorize(Policy = "CanManageRoles")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CreateRoleViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.RoleId = id;
            return View(model);
        }

        var role = new Role { Id = id, Name = model.Name, Description = model.Description };
        await _roleService.UpdateRoleAsync(role);

        await _auditLogService.LogAsync(GetCurrentUserId(), "Role Updated", "roles", GetClientIp(), GetUserAgent());

        TempData["SuccessMessage"] = "Роль обновлена";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "CanManageRoles")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await _roleService.DeleteRoleAsync(id))
        {
            TempData["ErrorMessage"] = "Невозможно удалить системную роль или роль с пользователями";
            return RedirectToAction(nameof(Index));
        }

        await _auditLogService.LogAsync(GetCurrentUserId(), "Role Deleted", "roles", GetClientIp(), GetUserAgent());

        TempData["SuccessMessage"] = "Роль удалена";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "CanManagePermissions")]
    [HttpGet]
    public async Task<IActionResult> ManagePermissions(int id)
    {
        var role = await _roleService.GetRoleByIdAsync(id);
        if (role == null) return NotFound();

        var allPermissions = await _roleService.GetAllPermissionsAsync();
        var selectedIds = await _roleService.GetRolePermissionIdsAsync(id);

        var groups = allPermissions
            .GroupBy(p => p.Resource)
            .Select(g => new PermissionGroup
            {
                Resource = g.Key,
                Permissions = g.Select(p => new PermissionItem
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description ?? string.Empty,
                    IsSelected = selectedIds.Contains(p.Id)
                }).ToList()
            }).ToList();

        var model = new ManagePermissionsViewModel
        {
            RoleId = role.Id,
            RoleName = role.Name,
            PermissionGroups = groups,
            SelectedPermissionIds = selectedIds
        };

        return View(model);
    }

    [Authorize(Policy = "CanManagePermissions")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManagePermissions(ManagePermissionsViewModel model)
    {
        await _roleService.ManagePermissionsAsync(model.RoleId, model.SelectedPermissionIds);

        await _auditLogService.LogAsync(GetCurrentUserId(), "Permissions Updated", "permissions", GetClientIp(), GetUserAgent(), details: $"Role: {model.RoleName}");

        TempData["SuccessMessage"] = "Разрешения обновлены";
        return RedirectToAction(nameof(Index));
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }

    private string? GetClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
    private string? GetUserAgent() => HttpContext.Request.Headers.UserAgent.ToString();
}
