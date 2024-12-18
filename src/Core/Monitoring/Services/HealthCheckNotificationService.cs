using System.Net.Http.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Core.Configuration;
using TradingSystem.Core.Monitoring.Interfaces;

namespace TradingSystem.Core.Monitoring.Services;

public class HealthCheckNotificationService : IHealthCheckNotificationService
{
    private readonly ILogger<HealthCheckNotificationService> _logger;
    private readonly MonitoringConfig _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Dictionary<string, DateTime> _lastNotifications;
    private readonly object _lock = new();

    public HealthCheckNotificationService(
        ILogger<HealthCheckNotificationService> logger,
        IOptions<MonitoringConfig> config,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _config = config.Value;
        _httpClientFactory = httpClientFactory;
        _lastNotifications = new Dictionary<string, DateTime>();
    }

    public async Task ProcessHealthCheckResultAsync(HealthReport report)
    {
        if (!_config.HealthChecks.Notifications.Enabled)
        {
            return;
        }

        try
        {
            foreach (var entry in report.Entries.Where(e => e.Value.Status != HealthStatus.Healthy))
            {
                // Check notification throttling
                var key = $"{entry.Key}_{entry.Value.Status}";
                var now = DateTime.UtcNow;

                lock (_lock)
                {
                    if (_lastNotifications.TryGetValue(key, out var lastNotification))
                    {
                        var timeSinceLastNotification = now - lastNotification;
                        if (timeSinceLastNotification.TotalSeconds < _config.HealthChecks.Notifications.MinimumSecondsBetweenNotifications)
                        {
                            _logger.LogDebug("Skipping notification for {Component} due to throttling", entry.Key);
                            continue;
                        }
                    }
                    _lastNotifications[key] = now;
                }

                await SendNotificationAsync(entry.Key, entry.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing health check result");
        }
    }

    public async Task SendNotificationAsync(string componentName, HealthReportEntry entry)
    {
        try
        {
            var notificationConfig = _config.HealthChecks.Notifications;

            foreach (var channel in notificationConfig.Channels)
            {
                try
                {
                    if (!ShouldSendNotification(channel, entry.Status))
                    {
                        continue;
                    }

                    switch (channel.Type.ToLowerInvariant())
                    {
                        case "email":
                            await SendEmailNotificationAsync(componentName, entry, channel);
                            break;
                        case "slack":
                            await SendSlackNotificationAsync(componentName, entry, channel);
                            break;
                        case "webhook":
                            await SendWebhookNotificationAsync(componentName, entry, channel);
                            break;
                        default:
                            _logger.LogWarning("Unknown notification channel type: {Type}", channel.Type);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending notification through channel {Channel}", channel.Name);
                }
            }

            _logger.LogInformation("Sent notifications for component {Component}", componentName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notifications for component {Component}", componentName);
        }
    }

    private bool ShouldSendNotification(NotificationChannelConfig channel, HealthStatus status)
    {
        // Check tag filters
        if (channel.Filter.IncludeTags.Any())
        {
            // TODO: Implement tag-based filtering
            return false;
        }

        // Check status filters
        if (channel.Filter.MinimumStatus.Any())
        {
            var statusHierarchy = new[] { "Healthy", "Degraded", "Unhealthy" };
            var minimumIndex = statusHierarchy.ToList().IndexOf(channel.Filter.MinimumStatus.First());
            var currentIndex = statusHierarchy.ToList().IndexOf(status.ToString());

            return currentIndex >= minimumIndex;
        }

        return true;
    }

    private async Task SendEmailNotificationAsync(string componentName, HealthReportEntry entry, NotificationChannelConfig channel)
    {
        if (!channel.Settings.TryGetValue("SmtpServer", out var smtpServer) || string.IsNullOrEmpty(smtpServer))
        {
            throw new InvalidOperationException("SMTP server not configured");
        }

        // TODO: Implement email notification logic
        await Task.CompletedTask;
    }

    private async Task SendSlackNotificationAsync(string componentName, HealthReportEntry entry, NotificationChannelConfig channel)
    {
        if (!channel.Settings.TryGetValue("WebhookUrl", out var webhookUrl) || string.IsNullOrEmpty(webhookUrl))
        {
            throw new InvalidOperationException("Slack webhook URL not configured");
        }

        var payload = new
        {
            text = FormatSlackMessage(componentName, entry),
            channel = channel.Settings.GetValueOrDefault("Channel", "#monitoring"),
            username = channel.Settings.GetValueOrDefault("Username", "Health Check Monitor")
        };

        using var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsJsonAsync(webhookUrl, payload);
        response.EnsureSuccessStatusCode();
    }

    private async Task SendWebhookNotificationAsync(string componentName, HealthReportEntry entry, NotificationChannelConfig channel)
    {
        if (!channel.Settings.TryGetValue("Url", out var webhookUrl) || string.IsNullOrEmpty(webhookUrl))
        {
            throw new InvalidOperationException("Webhook URL not configured");
        }

        var payload = new
        {
            component = componentName,
            status = entry.Status.ToString(),
            description = entry.Description,
            timestamp = DateTime.UtcNow,
            data = entry.Data
        };

        using var client = _httpClientFactory.CreateClient();
        
        if (channel.Settings.TryGetValue("AuthToken", out var authToken) && !string.IsNullOrEmpty(authToken))
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
        }

        var response = await client.PostAsJsonAsync(webhookUrl, payload);
        response.EnsureSuccessStatusCode();
    }

    private string FormatSlackMessage(string componentName, HealthReportEntry entry)
    {
        var status = entry.Status == HealthStatus.Healthy ? ":white_check_mark:" : ":x:";
        var message = $"{status} Health Check Alert: *{componentName}*\n";
        message += $"Status: *{entry.Status}*\n";
        message += $"Time: {DateTime.UtcNow:u}\n";

        if (!string.IsNullOrEmpty(entry.Description))
        {
            message += $"Description: {entry.Description}\n";
        }

        if (entry.Exception != null)
        {
            message += $"Error: {entry.Exception.Message}\n";
        }

        if (entry.Data.Any())
        {
            message += "Additional Data:\n";
            foreach (var (key, value) in entry.Data)
            {
                message += $"• {key}: {value}\n";
            }
        }

        return message;
    }
}
