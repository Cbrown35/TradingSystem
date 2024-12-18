namespace TradingSystem.Core.Monitoring.Models;

public class NotificationChannelConfig
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, string> Settings { get; set; } = new();
    public NotificationFilterConfig Filter { get; set; } = new();
}

public class NotificationFilterConfig
{
    public List<string> IncludeTags { get; set; } = new();
    public List<string> MinimumStatus { get; set; } = new();
}

public class NotificationTemplate
{
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string> Variables { get; set; } = new();
}

public class NotificationResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Channel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
