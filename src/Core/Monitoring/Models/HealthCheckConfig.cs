using System.Text.Json.Serialization;

namespace TradingSystem.Core.Monitoring.Models;

public class HealthCheckConfig
{
    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 30;
    public int TimeoutSeconds { get; set; } = 5;
    public List<HealthCheckEndpoint> Endpoints { get; set; } = new();
    public List<HealthCheckTag> Tags { get; set; } = new();
    public NotificationSettings Notifications { get; set; } = new();
    public UISettings UI { get; set; } = new();
    public StorageSettings Storage { get; set; } = new();
    public List<ThresholdConfig> Thresholds { get; set; } = new();
}

public class HealthCheckEndpoint
{
    public string Name { get; set; } = string.Empty;
    public string Uri { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public int Weight { get; set; } = 1;
    public HealthCheckEndpointConfig Config { get; set; } = new();
}

public class HealthCheckEndpointConfig
{
    public int TimeoutSeconds { get; set; } = 5;
    public int RetryCount { get; set; } = 3;
    public int RetryWaitSeconds { get; set; } = 5;
    public Dictionary<string, string> Headers { get; set; } = new();
}

public class HealthCheckTag
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#007bff";
    public List<string> HealthChecks { get; set; } = new();
}

public class NotificationSettings
{
    public bool Enabled { get; set; } = true;
    public int MinimumSecondsBetweenNotifications { get; set; } = 300;
    public List<NotificationChannel> Channels { get; set; } = new();
}

public class NotificationChannel
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, string> Settings { get; set; } = new();
    public NotificationFilter Filter { get; set; } = new();
}

public class NotificationFilter
{
    public List<string> IncludeTags { get; set; } = new();
    public List<string> ExcludeTags { get; set; } = new();
    public List<string> MinimumStatus { get; set; } = new();
}

public class UISettings
{
    public string PageTitle { get; set; } = "Trading System Health Status";
    public string CustomStylesheetUrl { get; set; } = "/css/healthchecks.css";
    public string CustomFaviconUrl { get; set; } = "/favicon.ico";
    public int RefreshIntervalSeconds { get; set; } = 30;
    public bool ShowDetailedErrors { get; set; } = true;
    public Dictionary<string, string> CustomLinks { get; set; } = new();
}

public class StorageSettings
{
    public string Type { get; set; } = "InMemory";
    public int RetentionDays { get; set; } = 90;
    public Dictionary<string, string> ConnectionStrings { get; set; } = new();
}

public class ThresholdConfig
{
    public string MetricName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ThresholdLevels Levels { get; set; } = new();
    public ThresholdActions Actions { get; set; } = new();
}

public class ThresholdLevels
{
    public decimal Warning { get; set; }
    public decimal Critical { get; set; }
}

public class ThresholdActions
{
    public List<string> Warning { get; set; } = new();
    public List<string> Critical { get; set; } = new();
}

public class HealthCheckResult
{
    public string Status { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public DateTime Timestamp { get; set; }
    public List<HealthCheckEntry> Entries { get; set; } = new();
}

public class HealthCheckEntry
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public string Error { get; set; } = string.Empty;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NotificationChannelType
{
    Email,
    Slack,
    PagerDuty,
    WebHook
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StorageType
{
    InMemory,
    SqlServer,
    PostgreSQL,
    SQLite
}

public static class HealthCheckDefaults
{
    public static readonly Dictionary<string, string> DefaultTags = new()
    {
        { "trading", "#28a745" },
        { "core", "#007bff" },
        { "infrastructure", "#6c757d" },
        { "cache", "#ffc107" },
        { "monitoring", "#17a2b8" }
    };

    public static readonly Dictionary<string, decimal[]> DefaultThresholds = new()
    {
        { "memory_usage", new[] { 80m, 90m } },
        { "cpu_usage", new[] { 80m, 90m } },
        { "disk_space", new[] { 80m, 90m } },
        { "error_rate", new[] { 0.01m, 0.05m } },
        { "latency", new[] { 0.5m, 1.0m } }
    };

    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan DefaultRetryInterval = TimeSpan.FromSeconds(5);
    public static readonly int DefaultRetryCount = 3;
    public static readonly int DefaultRefreshInterval = 30;
    public static readonly int DefaultRetentionDays = 90;
}
