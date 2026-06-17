using JwtAuthDemo.Infrastructure;
using JwtAuthDemo.Models;
using JwtAuthDemo.Services;
using JwtAuthDemo.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace JwtAuthDemo.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    [AllowAnonymous]
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction(nameof(Dashboard));
        return RedirectToAction(nameof(About));
    }

    [Authorize]
    public async Task<IActionResult> Dashboard()
    {
        var model = new DashboardViewModel
        {
            TotalUsers = await _context.Users.CountAsync(),
            ActiveToday = await _context.AuditLogs.CountAsync(a => a.CreatedAt.Date == DateTimeOffset.UtcNow.Date && a.Action.Contains("Login")),
            NewThisWeek = await _context.Users.CountAsync(u => u.CreatedAt >= DateTimeOffset.UtcNow.AddDays(-7)),
            OnlineNow = await _context.RefreshTokens.CountAsync(rt => rt.Revoked == null && rt.Expires > DateTimeOffset.UtcNow),
            RecentActivity = await _context.AuditLogs
                .Include(a => a.User)
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .Select(a => new ActivityLogEntry
                {
                    UserName = a.User != null ? a.User.UserName : "System",
                    Action = a.Action,
                    Resource = a.Resource,
                    CreatedAt = a.CreatedAt,
                    Status = a.Status
                }).ToListAsync(),
            RegistrationsChart = await _context.Users
                .Where(u => u.CreatedAt >= DateTimeOffset.UtcNow.AddDays(-30))
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new DailyRegistration
                {
                    Date = g.Key.ToString("dd.MM"),
                    Count = g.Count()
                }).ToListAsync(),
            RoleDistribution = await _context.UserRoles
                .GroupBy(ur => ur.Role.Name)
                .Select(g => new RoleDistribution
                {
                    RoleName = g.Key,
                    Count = g.Count()
                }).ToListAsync()
        };

        return View(model);
    }

    [AllowAnonymous]
    public IActionResult About() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
