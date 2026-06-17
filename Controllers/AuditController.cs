using JwtAuthDemo.Services;
using JwtAuthDemo.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JwtAuthDemo.Controllers;

[Authorize]
public class AuditController : Controller
{
    private readonly IAuditLogQueryableService _auditLogService;

    public AuditController(IAuditLogQueryableService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [Authorize(Policy = "CanManageAudit")]
    public async Task<IActionResult> Index(int page = 1, int? userId = null, string? action = null, DateTimeOffset? from = null, DateTimeOffset? to = null, string? status = null)
    {
        var pageSize = 20;
        var logs = await _auditLogService.GetLogsAsync(page, pageSize, userId, action, from, to, status);
        var totalCount = await _auditLogService.GetLogsCountAsync(userId, action, from, to, status);

        var model = new AuditLogViewModel
        {
            Logs = logs.Select(l => new AuditLogEntry
            {
                Id = l.Id,
                UserName = l.User?.UserName,
                Action = l.Action,
                Resource = l.Resource,
                IpAddress = l.IpAddress,
                Status = l.Status,
                Error = l.Error,
                CreatedAt = l.CreatedAt
            }).ToList(),
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            TotalItems = totalCount,
            FilterUserId = userId,
            FilterAction = action,
            FilterFrom = from,
            FilterTo = to,
            FilterStatus = status
        };

        return View(model);
    }
}
