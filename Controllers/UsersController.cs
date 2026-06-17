using JwtAuthDemo.Infrastructure;
using JwtAuthDemo.Models.Entities;
using JwtAuthDemo.Services;
using JwtAuthDemo.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JwtAuthDemo.Controllers;

[Authorize]
public class UsersController : Controller
{
    private readonly IUserService _userService;
    private readonly IAuditLogService _auditLogService;
    private readonly ApplicationDbContext _context;

    public UsersController(IUserService userService, IAuditLogService auditLogService, ApplicationDbContext context)
    {
        _userService = userService;
        _auditLogService = auditLogService;
        _context = context;
    }

    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string? search = null, string? role = null, string? status = null)
    {
        var users = await _userService.GetUsersAsync(page, pageSize, search, role, status);
        var totalCount = await _userService.GetUsersCountAsync(search, role, status);
        var roles = await _context.Roles.ToListAsync();

        ViewBag.Roles = roles;
        ViewBag.Search = search;
        ViewBag.RoleFilter = role;
        ViewBag.StatusFilter = status;
        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return View(users);
    }

    public async Task<IActionResult> Profile(int? id)
    {
        var userId = id ?? GetCurrentUserId();
        if (userId == null) return NotFound();

        var user = await _userService.GetUserByIdAsync(userId.Value);
        if (user == null) return NotFound();

        var logs = await _context.AuditLogs
            .Where(al => al.UserId == userId)
            .OrderByDescending(al => al.CreatedAt)
            .Take(20)
            .ToListAsync();

        var sessions = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.Revoked == null && rt.Expires > DateTimeOffset.UtcNow)
            .OrderByDescending(rt => rt.Created)
            .ToListAsync();

        ViewBag.AuditLogs = logs;
        ViewBag.Sessions = sessions;

        return View(user);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        var userId = id ?? GetCurrentUserId();
        if (userId == null) return NotFound();

        var user = await _userService.GetUserByIdAsync(userId.Value);
        if (user == null) return NotFound();

        var model = new EditProfileViewModel
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userService.GetUserByIdAsync(model.Id);
        if (user == null) return NotFound();

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.Email = model.Email;

        await _userService.UpdateUserAsync(user);

        await _auditLogService.LogAsync(user.Id, "Profile Updated", "profile", GetClientIp(), GetUserAgent());

        TempData["SuccessMessage"] = "Профиль обновлен";
        return RedirectToAction(nameof(Profile), new { id = model.Id });
    }

    [Authorize(Policy = "CanManageUsers")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Roles = await _context.Roles.ToListAsync();
        return View();
    }

    [Authorize(Policy = "CanManageUsers")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = await _context.Roles.ToListAsync();
            return View(model);
        }

        var user = new User
        {
            UserName = model.UserName,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName
        };

        if (!await _userService.CreateUserAsync(user, model.Password, model.SelectedRoleIds))
        {
            ModelState.AddModelError(string.Empty, "Пользователь уже существует");
            ViewBag.Roles = await _context.Roles.ToListAsync();
            return View(model);
        }

        await _auditLogService.LogAsync(GetCurrentUserId(), "User Created", "users", GetClientIp(), GetUserAgent(), details: $"Created user: {model.UserName}");

        TempData["SuccessMessage"] = "Пользователь создан";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "CanManageUsers")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null) return NotFound();

        await _userService.DeleteUserAsync(id);
        await _auditLogService.LogAsync(GetCurrentUserId(), "User Deleted", "users", GetClientIp(), GetUserAgent(), details: $"Deleted user: {user.UserName}");

        TempData["SuccessMessage"] = "Пользователь удален";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "CanManageUsers")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(int id)
    {
        await _userService.ToggleLockUserAsync(id);
        await _auditLogService.LogAsync(GetCurrentUserId(), "User Lock Toggled", "users", GetClientIp(), GetUserAgent());

        TempData["SuccessMessage"] = "Статус блокировки изменен";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "CanManageUsers")]
    [HttpGet]
    public async Task<IActionResult> AssignRoles(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null) return NotFound();

        var allRoles = await _context.Roles.ToListAsync();
        var userRoleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();

        var model = new AssignRolesViewModel
        {
            UserId = user.Id,
            UserName = user.UserName,
            SelectedRoleIds = userRoleIds
        };

        ViewBag.AllRoles = allRoles;
        return View(model);
    }

    [Authorize(Policy = "CanManageUsers")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRoles(AssignRolesViewModel model)
    {
        await _userService.AssignRolesAsync(model.UserId, model.SelectedRoleIds);
        await _auditLogService.LogAsync(GetCurrentUserId(), "Roles Assigned", "users", GetClientIp(), GetUserAgent());

        TempData["SuccessMessage"] = "Роли назначены";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "CanManageUsers")]
    public async Task<IActionResult> Details(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null) return NotFound();
        return View(user);
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }

    private string? GetClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? GetUserAgent() => HttpContext.Request.Headers.UserAgent.ToString();
}
