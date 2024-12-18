using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TradingSystem.Core.Monitoring.Models;

public class HealthCheckAnalytics
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int TotalChecks { get; set; }
    public int HealthyChecks { get; set; }
    public int UnhealthyChecks { get; set; }
    public double AverageDuration { get; set; }
    public List<HealthCheckIssue> Issues { get; set; } = new();
}

public class HealthCheckIssue
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime LastOccurrence { get; set; }
}

public class HealthCheckRequestLog
{
    public DateTime Timestamp { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public TimeSpan Duration { get; set; }
    public string UserAgent { get; set; } = string.Empty;
    public string ClientIp { get; set; } = string.Empty;
}

public interface IHealthCheckStorageService
{
    Task StoreHealthCheckResultAsync(HealthReport result);
    Task<IEnumerable<HealthReport>> GetHealthCheckHistoryAsync(DateTime startTime, DateTime endTime);
    Task CleanupOldEntriesAsync();
}

public interface IHealthCheckNotificationService
{
    Task ProcessHealthCheckResultAsync(HealthReport result);
    Task SendNotificationAsync(string componentName, HealthReportEntry entry);
}

public class NotificationsConfig
{
    public bool Enabled { get; set; }
    public EmailSettings EmailSettings { get; set; } = new();
    public SlackSettings SlackSettings { get; set; } = new();
    public WebhookSettings WebhookSettings { get; set; } = new();
}

public class EmailSettings
{
    public bool Enabled { get; set; }
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public List<string> ToAddresses { get; set; } = new();
}

public class SlackSettings
{
    public bool Enabled { get; set; }
    public string WebhookUrl { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
}

public class WebhookSettings
{
    public bool Enabled { get; set; }
    public string Url { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
}
