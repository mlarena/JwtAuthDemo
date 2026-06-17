using JwtAuthDemo.Infrastructure;
using JwtAuthDemo.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace JwtAuthDemo.Services;

public interface IRoleService
{
    Task<List<Role>> GetAllRolesAsync();
    Task<Role?> GetRoleByIdAsync(int id);
    Task<bool> CreateRoleAsync(Role role);
    Task<bool> UpdateRoleAsync(Role role);
    Task<bool> DeleteRoleAsync(int id);
    Task<bool> ManagePermissionsAsync(int roleId, List<int> permissionIds);
    Task<List<Permission>> GetAllPermissionsAsync();
    Task<List<int>> GetRolePermissionIdsAsync(int roleId);
    Task<int> GetRoleUserCountAsync(int roleId);
}

public class RoleService : IRoleService
{
    private readonly ApplicationDbContext _context;

    public RoleService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Role>> GetAllRolesAsync()
        => await _context.Roles.Include(r => r.RolePermissions).ToListAsync();

    public async Task<Role?> GetRoleByIdAsync(int id)
        => await _context.Roles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.Id == id);

    public async Task<bool> CreateRoleAsync(Role role)
    {
        if (await _context.Roles.AnyAsync(r => r.Name == role.Name))
            return false;

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateRoleAsync(Role role)
    {
        var existing = await _context.Roles.FindAsync(role.Id);
        if (existing == null || existing.IsSystem) return false;

        existing.Name = role.Name;
        existing.Description = role.Description;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteRoleAsync(int id)
    {
        var role = await _context.Roles.Include(r => r.UserRoles).FirstOrDefaultAsync(r => r.Id == id);
        if (role == null || role.IsSystem) return false;
        if (role.UserRoles.Any()) return false;

        _context.Roles.Remove(role);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ManagePermissionsAsync(int roleId, List<int> permissionIds)
    {
        var existing = await _context.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
        _context.RolePermissions.RemoveRange(existing);

        foreach (var permissionId in permissionIds)
        {
            _context.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Permission>> GetAllPermissionsAsync()
        => await _context.Permissions.OrderBy(p => p.Resource).ThenBy(p => p.Action).ToListAsync();

    public async Task<List<int>> GetRolePermissionIdsAsync(int roleId)
        => await _context.RolePermissions.Where(rp => rp.RoleId == roleId).Select(rp => rp.PermissionId).ToListAsync();

    public async Task<int> GetRoleUserCountAsync(int roleId)
        => await _context.UserRoles.CountAsync(ur => ur.RoleId == roleId);
}
