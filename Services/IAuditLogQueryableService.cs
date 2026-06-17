using JwtAuthDemo.Infrastructure;
using JwtAuthDemo.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace JwtAuthDemo.Services;

public interface IAuditLogQueryableService
{
    Task<List<AuditLog>> GetLogsAsync(int page, int pageSize, int? userId, string? action, DateTimeOffset? from, DateTimeOffset? to, string? status);
    Task<int> GetLogsCountAsync(int? userId, string? action, DateTimeOffset? from, DateTimeOffset? to, string? status);
    Task CleanupOldLogsAsync(int daysOld = 90);
}

public class AuditLogQueryableService : IAuditLogQueryableService
{
    private readonly ApplicationDbContext _context;

    public AuditLogQueryableService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<AuditLog>> GetLogsAsync(int page, int pageSize, int? userId, string? action, DateTimeOffset? from, DateTimeOffset? to, string? status)
    {
        var query = _context.AuditLogs.Include(al => al.User).AsQueryable();

        if (userId.HasValue)
            query = query.Where(al => al.UserId == userId.Value);

        if (!string.IsNullOrEmpty(action))
            query = query.Where(al => al.Action.Contains(action));

        if (from.HasValue)
            query = query.Where(al => al.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(al => al.CreatedAt <= to.Value);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(al => al.Status == status);

        return await query.OrderByDescending(al => al.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<int> GetLogsCountAsync(int? userId, string? action, DateTimeOffset? from, DateTimeOffset? to, string? status)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (userId.HasValue)
            query = query.Where(al => al.UserId == userId.Value);

        if (!string.IsNullOrEmpty(action))
            query = query.Where(al => al.Action.Contains(action));

        if (from.HasValue)
            query = query.Where(al => al.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(al => al.CreatedAt <= to.Value);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(al => al.Status == status);

        return await query.CountAsync();
    }

    public async Task CleanupOldLogsAsync(int daysOld = 90)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-daysOld);
        var oldLogs = await _context.AuditLogs.Where(al => al.CreatedAt < cutoff).ToListAsync();
        _context.AuditLogs.RemoveRange(oldLogs);
        await _context.SaveChangesAsync();
    }
}
