using JwtAuthDemo.Infrastructure;
using JwtAuthDemo.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace JwtAuthDemo.Services;

public interface IAuditLogService
{
    Task LogAsync(int? userId, string action, string? resource, string? ipAddress, string? userAgent, string status = "Success", string? error = null, string? details = null);
}

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;

    public AuditLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(int? userId, string action, string? resource, string? ipAddress, string? userAgent, string status = "Success", string? error = null, string? details = null)
    {
        var log = new AuditLog
        {
            UserId = userId,
            Action = action,
            Resource = resource,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Status = status,
            Error = error,
            Details = details,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}
