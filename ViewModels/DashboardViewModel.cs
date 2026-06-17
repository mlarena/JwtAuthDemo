namespace JwtAuthDemo.ViewModels;

public class DashboardViewModel
{
    public int TotalUsers { get; set; }
    public int ActiveToday { get; set; }
    public int NewThisWeek { get; set; }
    public int OnlineNow { get; set; }
    public List<ActivityLogEntry> RecentActivity { get; set; } = new();
    public List<DailyRegistration> RegistrationsChart { get; set; } = new();
    public List<RoleDistribution> RoleDistribution { get; set; } = new();
}

public class ActivityLogEntry
{
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Resource { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class DailyRegistration
{
    public string Date { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class RoleDistribution
{
    public string RoleName { get; set; } = string.Empty;
    public int Count { get; set; }
}
