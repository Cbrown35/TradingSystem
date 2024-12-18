using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TradingSystem.Common.Models;

public class HealthCheckHistoryEntry
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Status { get; set; } = string.Empty;
    public TimeSpan TotalDuration { get; set; }
    public string Results { get; set; } = string.Empty;
}

public class HealthCheckNotification
{
    public string ComponentName { get; set; } = string.Empty;
    public HealthStatus Status { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public TimeSpan Duration { get; set; }
    public string? Exception { get; set; }
    public IReadOnlyDictionary<string, object>? Data { get; set; }
}
