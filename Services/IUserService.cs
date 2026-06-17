using JwtAuthDemo.Infrastructure;
using JwtAuthDemo.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace JwtAuthDemo.Services;

public interface IUserService
{
    Task<User?> GetUserByIdAsync(int id);
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> GetUserByUserNameAsync(string userName);
    Task<List<User>> GetUsersAsync(int page, int pageSize, string? search, string? roleFilter, string? statusFilter);
    Task<int> GetUsersCountAsync(string? search, string? roleFilter, string? statusFilter);
    Task<bool> CreateUserAsync(User user, string password, List<int> roleIds);
    Task<bool> UpdateUserAsync(User user);
    Task<bool> DeleteUserAsync(int id);
    Task<bool> ToggleLockUserAsync(int id);
    Task<bool> AssignRolesAsync(int userId, List<int> roleIds);
    Task<List<string>> GetUserRolesAsync(int userId);
    Task<List<string>> GetUserPermissionsAsync(int userId);
}

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;

    public UserService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetUserByIdAsync(int id)
        => await _context.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).FirstOrDefaultAsync(u => u.Id == id);

    public async Task<User?> GetUserByEmailAsync(string email)
        => await _context.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> GetUserByUserNameAsync(string userName)
        => await _context.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.UserName == userName);

    public async Task<List<User>> GetUsersAsync(int page, int pageSize, string? search, string? roleFilter, string? statusFilter)
    {
        var query = _context.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(u => u.UserName.Contains(search) || u.Email.Contains(search) || (u.FirstName != null && u.FirstName.Contains(search)) || (u.LastName != null && u.LastName.Contains(search)));

        if (!string.IsNullOrEmpty(roleFilter))
            query = query.Where(u => u.UserRoles.Any(ur => ur.Role.Name == roleFilter));

        if (statusFilter == "active")
            query = query.Where(u => u.IsActive);
        else if (statusFilter == "inactive")
            query = query.Where(u => !u.IsActive);
        else if (statusFilter == "locked")
            query = query.Where(u => u.IsLocked);

        return await query.OrderBy(u => u.UserName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<int> GetUsersCountAsync(string? search, string? roleFilter, string? statusFilter)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(u => u.UserName.Contains(search) || u.Email.Contains(search));

        if (!string.IsNullOrEmpty(roleFilter))
            query = query.Where(u => u.UserRoles.Any(ur => ur.Role.Name == roleFilter));

        if (statusFilter == "active")
            query = query.Where(u => u.IsActive);
        else if (statusFilter == "inactive")
            query = query.Where(u => !u.IsActive);
        else if (statusFilter == "locked")
            query = query.Where(u => u.IsLocked);

        return await query.CountAsync();
    }

    public async Task<bool> CreateUserAsync(User user, string password, List<int> roleIds)
    {
        if (await _context.Users.AnyAsync(u => u.UserName == user.UserName || u.Email == user.Email))
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        foreach (var roleId in roleIds)
        {
            _context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
        }
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateUserAsync(User user)
    {
        var existing = await _context.Users.FindAsync(user.Id);
        if (existing == null) return false;

        existing.FirstName = user.FirstName;
        existing.LastName = user.LastName;
        existing.Email = user.Email;
        existing.IsActive = user.IsActive;
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleLockUserAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        user.IsLocked = !user.IsLocked;
        user.LockoutEnd = user.IsLocked ? DateTimeOffset.UtcNow.AddYears(1) : null;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AssignRolesAsync(int userId, List<int> roleIds)
    {
        var userRoles = await _context.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();
        _context.UserRoles.RemoveRange(userRoles);

        foreach (var roleId in roleIds)
        {
            _context.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<string>> GetUserRolesAsync(int userId)
        => await _context.UserRoles.Where(ur => ur.UserId == userId).Select(ur => ur.Role.Name).ToListAsync();

    public async Task<List<string>> GetUserPermissionsAsync(int userId)
        => await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync();
}
