using System.Text.Json.Serialization;

namespace TradingSystem.Core.Configuration;

public class MonitoringConfig
{
    public HealthChecksConfig HealthChecks { get; set; } = new();
    public MetricsConfig Metrics { get; set; } = new();
    public DevelopmentConfig? Development { get; set; }
}

public class HealthChecksConfig
{
    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 30;
    public int TimeoutSeconds { get; set; } = 5;
    public UIConfig UI { get; set; } = new();
    public CacheConfig Cache { get; set; } = new();
    public RateLimitConfig RateLimit { get; set; } = new();
    public CompressionConfig Compression { get; set; } = new();
    public LoggingConfig Logging { get; set; } = new();
    public List<TagConfig> Tags { get; set; } = new();
    public NotificationsConfig Notifications { get; set; } = new();
}

public class UIConfig
{
    public string Path { get; set; } = "/healthchecks-ui";
    public string ApiPath { get; set; } = "/healthchecks-api";
    public string PageTitle { get; set; } = "Trading System Health Status";
    public string CustomStylesheetUrl { get; set; } = "/css/healthchecks.css";
    public bool ShowDetailedErrors { get; set; } = true;
    public AuthenticationConfig Authentication { get; set; } = new();
}

public class AuthenticationConfig
{
    public bool Enabled { get; set; } = true;
    public string Provider { get; set; } = "basic";
    public BasicAuthConfig? BasicAuth { get; set; }
}

public class BasicAuthConfig
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class CacheConfig
{
    public bool Enabled { get; set; } = true;
    public bool UseRedis { get; set; } = true;
    public string RedisConnectionString { get; set; } = "localhost:6379";
    public int TimeToLiveSeconds { get; set; } = 30;
    public int SlidingExpirationSeconds { get; set; } = 10;
}

public class RateLimitConfig
{
    public bool Enabled { get; set; } = true;
    public int RequestsPerMinute { get; set; } = 60;
    public int BurstSize { get; set; } = 10;
    public List<string> WhitelistedIPs { get; set; } = new();
    public List<string> WhitelistedUsers { get; set; } = new();
}

public class CompressionConfig
{
    public bool Enabled { get; set; } = true;
    public string Level { get; set; } = "optimal";
    public int MinimumSizeBytes { get; set; } = 1024;
    public bool PreferBrotli { get; set; } = true;
}

public class LoggingConfig
{
    public bool Enabled { get; set; } = true;
    public bool StoreInDatabase { get; set; } = true;
    public bool WriteToFile { get; set; } = true;
    public string LogFilePath { get; set; } = "logs/health-checks.log";
}

public class TagConfig
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public List<string> HealthChecks { get; set; } = new();
}

public class NotificationsConfig
{
    public bool Enabled { get; set; } = true;
    public int MinimumSecondsBetweenNotifications { get; set; } = 300;
    public List<NotificationChannelConfig> Channels { get; set; } = new();
}

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

public class MetricsConfig
{
    public bool Enabled { get; set; } = true;
    public TimeSpan CollectionInterval { get; set; } = TimeSpan.FromSeconds(15);
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(30);
    public string PrometheusEndpoint { get; set; } = "/metrics";
    public int ExporterPort { get; set; } = 9090;
    public MetricsDevelopmentConfig? Development { get; set; }
}

public class MetricsDevelopmentConfig
{
    public bool EnableDetailedMetrics { get; set; } = true;
    public bool EnableDebugEndpoints { get; set; } = true;
    public string MetricsPath { get; set; } = "/metrics-debug";
}

public class DevelopmentConfig
{
    public bool MockExchanges { get; set; } = true;
    public bool SimulatedTrading { get; set; } = true;
    public bool LocalDataFeeds { get; set; } = true;
    public bool DebugLogging { get; set; } = true;
    public bool MockNotifications { get; set; } = true;
    public bool DisableRealMoneyTrading { get; set; } = true;
    public TestAccountsConfig TestAccounts { get; set; } = new();
    public EndpointsConfig Endpoints { get; set; } = new();
}

public class TestAccountsConfig
{
    public string Exchange { get; set; } = string.Empty;
    public string MarketData { get; set; } = string.Empty;
    public string Notifications { get; set; } = string.Empty;
}

public class EndpointsConfig
{
    public string Exchange { get; set; } = string.Empty;
    public string MarketData { get; set; } = string.Empty;
    public string RiskManagement { get; set; } = string.Empty;
}
