using JwtAuthDemo.Models.Entities;
using JwtAuthDemo.Services;
using JwtAuthDemo.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using JwtAuthDemo.Infrastructure;

namespace JwtAuthDemo.Controllers;

[Authorize]
public class AccountController : Controller
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUserService _userService;
    private readonly IAuditLogService _auditLogService;
    private readonly IAuditLogQueryableService _auditLogQueryableService;
    private readonly ApplicationDbContext _context;

    public AccountController(IJwtTokenService jwtTokenService, IUserService userService, IAuditLogService auditLogService, IAuditLogQueryableService auditLogQueryableService, ApplicationDbContext context)
    {
        _jwtTokenService = jwtTokenService;
        _userService = userService;
        _auditLogService = auditLogService;
        _auditLogQueryableService = auditLogQueryableService;
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Dashboard", "Home");

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userService.GetUserByEmailAsync(model.UserNameOrEmail)
            ?? await _userService.GetUserByUserNameAsync(model.UserNameOrEmail);

        if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
        {
            await _auditLogService.LogAsync(null, "Login Failed", "auth", GetClientIp(), GetUserAgent(), "Failed", "Invalid credentials");
            ModelState.AddModelError(string.Empty, "Неверное имя пользователя или пароль");
            return View(model);
        }

        if (user.IsLocked)
        {
            ModelState.AddModelError(string.Empty, "Аккаунт заблокирован");
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Аккаунт деактивирован");
            return View(model);
        }

        var roles = await _userService.GetUserRolesAsync(user.Id);
        var permissions = await _userService.GetUserPermissionsAsync(user.Id);

        var accessToken = _jwtTokenService.GenerateAccessToken(user, roles, permissions);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        _context.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            CreatedByIp = GetClientIp()
        });

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        SetJwtCookie(accessToken);

        await _auditLogService.LogAsync(user.Id, "Login Success", "auth", GetClientIp(), GetUserAgent());

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Dashboard", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register() => View();

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (!model.AgreeToTerms)
        {
            ModelState.AddModelError(string.Empty, "Необходимо принять условия");
            return View(model);
        }

        var user = new User
        {
            UserName = model.UserName,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            EmailConfirmationToken = Guid.NewGuid().ToString("N"),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        if (!await _userService.CreateUserAsync(user, model.Password, new List<int> { 4 }))
        {
            ModelState.AddModelError(string.Empty, "Пользователь с таким именем или email уже существует");
            return View(model);
        }

        await _auditLogService.LogAsync(user.Id, "User Registered", "auth", GetClientIp(), GetUserAgent());

        TempData["SuccessMessage"] = "Регистрация успешна. Войдите в систему.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userId = GetCurrentUserId();

        Response.Cookies.Delete("jwt");

        if (userId.HasValue)
        {
            var refreshTokens = await _context.RefreshTokens.Where(rt => rt.UserId == userId.Value).ToListAsync();
            _context.RefreshTokens.RemoveRange(refreshTokens);
            await _context.SaveChangesAsync();

            await _auditLogService.LogAsync(userId, "Logout", "auth", GetClientIp(), GetUserAgent());
        }

        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string token, string email)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            return BadRequest();

        var user = await _userService.GetUserByEmailAsync(email);
        if (user == null || user.EmailConfirmationToken != token)
            return BadRequest();

        user.IsEmailConfirmed = true;
        user.EmailConfirmationToken = null;
        await _userService.UpdateUserAsync(user);

        await _auditLogService.LogAsync(user.Id, "Email Confirmed", "auth", GetClientIp(), GetUserAgent());

        TempData["SuccessMessage"] = "Email подтвержден. Теперь вы можете войти.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword() => View();

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userService.GetUserByEmailAsync(model.Email);
        if (user != null)
        {
            user.PasswordResetToken = Guid.NewGuid().ToString("N");
            user.PasswordResetTokenExpiry = DateTimeOffset.UtcNow.AddHours(1);
            await _userService.UpdateUserAsync(user);

            await _auditLogService.LogAsync(user.Id, "Password Reset Requested", "auth", GetClientIp(), GetUserAgent());
        }

        TempData["SuccessMessage"] = "Если аккаунт существует, письмо с инструкциями отправлено.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string token) => View(new ResetPasswordViewModel { Token = token ?? string.Empty });

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.Email == model.Email && u.PasswordResetToken == model.Token &&
            u.PasswordResetTokenExpiry > DateTimeOffset.UtcNow);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Неверный или истекший токен");
            return View(model);
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(user.Id, "Password Reset Completed", "auth", GetClientIp(), GetUserAgent());

        TempData["SuccessMessage"] = "Пароль успешно сброшен. Войдите в систему.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RefreshToken()
    {
        var jwtToken = Request.Cookies["jwt"];
        if (string.IsNullOrEmpty(jwtToken))
            return RedirectToAction(nameof(Login));

        var principal = _jwtTokenService.GetPrincipalFromExpiredToken(jwtToken);
        if (principal == null)
            return RedirectToAction(nameof(Login));

        var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
            return RedirectToAction(nameof(Login));

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null || user.IsLocked || !user.IsActive)
            return RedirectToAction(nameof(Login));

        var roles = await _userService.GetUserRolesAsync(user.Id);
        var permissions = await _userService.GetUserPermissionsAsync(user.Id);

        var newAccessToken = _jwtTokenService.GenerateAccessToken(user, roles, permissions);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

        var oldRefreshTokens = await _context.RefreshTokens.Where(rt => rt.UserId == userId).ToListAsync();
        foreach (var old in oldRefreshTokens)
            old.Revoked = DateTimeOffset.UtcNow;

        _context.RefreshTokens.Add(new RefreshToken
        {
            Token = newRefreshToken,
            UserId = user.Id,
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            CreatedByIp = GetClientIp()
        });

        await _context.SaveChangesAsync();

        SetJwtCookie(newAccessToken);

        return RedirectToAction("Dashboard", "Home");
    }

    private void SetJwtCookie(string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(60),
            Path = "/"
        };
        Response.Cookies.Append("jwt", token, cookieOptions);
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }

    private string? GetClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? GetUserAgent() => HttpContext.Request.Headers.UserAgent.ToString();
}
