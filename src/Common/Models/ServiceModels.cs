namespace TradingSystem.Common.Models;

public class ServiceStatus
{
    public bool IsConnected { get; set; }
    public bool IsOperational { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
    public Dictionary<string, object> Metrics { get; set; } = new();
    public DateTime LastUpdateTime { get; set; } = DateTime.UtcNow;
}

public class ServiceMetrics
{
    public TimeSpan Latency { get; set; }
    public int RequestCount { get; set; }
    public int ErrorCount { get; set; }
    public Dictionary<string, decimal> CustomMetrics { get; set; } = new();
}

public class HealthCheckOptions
{
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool IncludeMetrics { get; set; } = true;
    public string[] RequiredServices { get; set; } = Array.Empty<string>();
}
