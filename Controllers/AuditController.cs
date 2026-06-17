using JwtAuthDemo.ViewModels;
using JwtAuthDemo.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JwtAuthDemo.Controllers;

[Authorize]
[Authorize(Policy = "CanManageAudit")]
public class AuditController : Controller
{
    private readonly IAuditLogQueryableService _auditService;

    public AuditController(IAuditLogQueryableService auditService)
    {
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(int? userId, string? action, string? from, string? to, string? status, int page = 1)
    {
        const int pageSize = 20;

        DateTimeOffset? fromDate = null;
        DateTimeOffset? toDate = null;

        if (DateTimeOffset.TryParse(from, out var parsedFrom))
            fromDate = parsedFrom;
        if (DateTimeOffset.TryParse(to, out var parsedTo))
            toDate = parsedTo;

        var logs = await _auditService.GetLogsAsync(page, pageSize, userId, action, fromDate, toDate, status);
        var totalCount = await _auditService.GetLogsCountAsync(userId, action, fromDate, toDate, status);

        var viewModel = new AuditLogViewModel
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
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            TotalItems = totalCount,
            FilterUserId = userId,
            FilterAction = action,
            FilterFrom = fromDate,
            FilterTo = toDate,
            FilterStatus = status
        };

        return View(viewModel);
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? int.Parse(claim.Value) : null;
    }

    private string? GetClientIp()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
