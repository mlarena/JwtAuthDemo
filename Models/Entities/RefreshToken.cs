using System.ComponentModel.DataAnnotations;

namespace JwtAuthDemo.Models.Entities;

public class RefreshToken
{
    public int Id { get; set; }

    [Required]
    public string Token { get; set; } = string.Empty;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTimeOffset Expires { get; set; }

    public DateTimeOffset Created { get; set; }

    [MaxLength(45)]
    public string? CreatedByIp { get; set; }

    public DateTimeOffset? Revoked { get; set; }

    [MaxLength(45)]
    public string? RevokedByIp { get; set; }

    public string? ReplacedByToken { get; set; }

    public bool IsActive => Revoked == null && !IsExpired;

    public bool IsExpired => DateTimeOffset.UtcNow >= Expires;
}
