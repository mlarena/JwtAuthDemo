using JwtAuthDemo.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace JwtAuthDemo.Infrastructure;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId);

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId);

        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId);

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId);

        modelBuilder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId);

        modelBuilder.Entity<AuditLog>()
            .HasOne(al => al.User)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(al => al.UserId);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.UserName)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Permission>()
            .HasIndex(p => p.Name)
            .IsUnique();

        modelBuilder.Entity<Role>()
            .HasIndex(r => r.Name)
            .IsUnique();

        var now = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        modelBuilder.Entity<User>()
            .Property(u => u.CreatedAt)
            .HasDefaultValue(now);

        modelBuilder.Entity<Role>()
            .Property(r => r.CreatedAt)
            .HasDefaultValue(now);

        modelBuilder.Entity<RefreshToken>()
            .Property(rt => rt.Created)
            .HasDefaultValue(now);

        modelBuilder.Entity<AuditLog>()
            .Property(al => al.CreatedAt)
            .HasDefaultValue(now);

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "SuperAdmin", Description = "Полный доступ", IsSystem = true },
            new Role { Id = 2, Name = "Admin", Description = "Управление системой", IsSystem = true },
            new Role { Id = 3, Name = "Manager", Description = "Управление контентом", IsSystem = false },
            new Role { Id = 4, Name = "User", Description = "Обычный пользователь", IsSystem = false },
            new Role { Id = 5, Name = "Guest", Description = "Гостевой доступ", IsSystem = false }
        );

        modelBuilder.Entity<Permission>().HasData(
            new Permission { Id = 1, Name = "users:read", Resource = "users", Action = "read", Description = "Просмотр пользователей" },
            new Permission { Id = 2, Name = "users:create", Resource = "users", Action = "create", Description = "Создание пользователей" },
            new Permission { Id = 3, Name = "users:update", Resource = "users", Action = "update", Description = "Редактирование пользователей" },
            new Permission { Id = 4, Name = "users:delete", Resource = "users", Action = "delete", Description = "Удаление пользователей" },
            new Permission { Id = 5, Name = "users:lock", Resource = "users", Action = "lock", Description = "Блокировка пользователей" },
            new Permission { Id = 6, Name = "roles:read", Resource = "roles", Action = "read", Description = "Просмотр ролей" },
            new Permission { Id = 7, Name = "roles:create", Resource = "roles", Action = "create", Description = "Создание ролей" },
            new Permission { Id = 8, Name = "roles:update", Resource = "roles", Action = "update", Description = "Редактирование ролей" },
            new Permission { Id = 9, Name = "roles:delete", Resource = "roles", Action = "delete", Description = "Удаление ролей" },
            new Permission { Id = 10, Name = "permissions:manage", Resource = "permissions", Action = "manage", Description = "Управление правами" },
            new Permission { Id = 11, Name = "audit:read", Resource = "audit", Action = "read", Description = "Просмотр логов" },
            new Permission { Id = 12, Name = "profile:edit", Resource = "profile", Action = "edit", Description = "Редактирование профиля" },
            new Permission { Id = 13, Name = "dashboard:read", Resource = "dashboard", Action = "read", Description = "Просмотр дашборда" }
        );

        modelBuilder.Entity<RolePermission>().HasData(
            new RolePermission { RoleId = 1, PermissionId = 1 },
            new RolePermission { RoleId = 1, PermissionId = 2 },
            new RolePermission { RoleId = 1, PermissionId = 3 },
            new RolePermission { RoleId = 1, PermissionId = 4 },
            new RolePermission { RoleId = 1, PermissionId = 5 },
            new RolePermission { RoleId = 1, PermissionId = 6 },
            new RolePermission { RoleId = 1, PermissionId = 7 },
            new RolePermission { RoleId = 1, PermissionId = 8 },
            new RolePermission { RoleId = 1, PermissionId = 9 },
            new RolePermission { RoleId = 1, PermissionId = 10 },
            new RolePermission { RoleId = 1, PermissionId = 11 },
            new RolePermission { RoleId = 1, PermissionId = 12 },
            new RolePermission { RoleId = 1, PermissionId = 13 },

            new RolePermission { RoleId = 2, PermissionId = 1 },
            new RolePermission { RoleId = 2, PermissionId = 2 },
            new RolePermission { RoleId = 2, PermissionId = 3 },
            new RolePermission { RoleId = 2, PermissionId = 4 },
            new RolePermission { RoleId = 2, PermissionId = 5 },
            new RolePermission { RoleId = 2, PermissionId = 6 },
            new RolePermission { RoleId = 2, PermissionId = 7 },
            new RolePermission { RoleId = 2, PermissionId = 8 },
            new RolePermission { RoleId = 2, PermissionId = 9 },
            new RolePermission { RoleId = 2, PermissionId = 11 },

            new RolePermission { RoleId = 3, PermissionId = 1 },
            new RolePermission { RoleId = 3, PermissionId = 3 },
            new RolePermission { RoleId = 3, PermissionId = 13 },

            new RolePermission { RoleId = 4, PermissionId = 12 },
            new RolePermission { RoleId = 4, PermissionId = 13 },

            new RolePermission { RoleId = 5, PermissionId = 13 }
        );
    }
}
