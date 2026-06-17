using System.ComponentModel.DataAnnotations;

namespace JwtAuthDemo.ViewModels;

public class AuditLogViewModel
{
    public List<AuditLogEntry> Logs { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }

    [Display(Name = "Пользователь")]
    public int? FilterUserId { get; set; }

    [Display(Name = "Действие")]
    public string? FilterAction { get; set; }

    public DateTimeOffset? FilterFrom { get; set; }
    public DateTimeOffset? FilterTo { get; set; }

    [Display(Name = "Статус")]
    public string? FilterStatus { get; set; }
}

public class AuditLogEntry
{
    public long Id { get; set; }
    public string? UserName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Resource { get; set; }
    public string? IpAddress { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Error { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
