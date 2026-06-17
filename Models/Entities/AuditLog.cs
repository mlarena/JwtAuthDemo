using System.ComponentModel.DataAnnotations;

namespace JwtAuthDemo.Models.Entities;

public class AuditLog
{
    public long Id { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }

    [Required, MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Resource { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Success";

    [MaxLength(500)]
    public string? Error { get; set; }

    public string? Details { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
