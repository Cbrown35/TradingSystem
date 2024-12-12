using System.Net.Http.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using TradingSystem.Core.Configuration;
using TradingSystem.Core.Monitoring.Models;

namespace TradingSystem.Core.Monitoring.Services;

public class HealthCheckNotificationService
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

    public async Task SendNotifications(HealthCheckResult result)
    {
        if (!_config.HealthChecks.Notifications.Enabled)
        {
            return;
        }

        try
        {
            foreach (var channel in _config.HealthChecks.Notifications.Channels)
            {
                if (ShouldSendNotification(channel, result))
                {
                    await SendNotification(channel, result);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending health check notifications");
        }
    }

    public async Task SendNotification(NotificationChannelConfig channel, HealthCheckResult result)
    {
        try
        {
            switch (channel.Type.ToLower())
            {
                case "slack":
                    await SendSlackNotification(channel, result);
                    break;
                case "email":
                    await SendEmailNotification(channel, result);
                    break;
                case "pagerduty":
                    await SendPagerDutyNotification(channel, result);
                    break;
                default:
                    _logger.LogWarning("Unsupported notification channel type: {Type}", channel.Type);
                    break;
            }

            UpdateLastNotificationTime(channel.Name);
            _logger.LogInformation("Sent health check notification via {Channel}", channel.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification via {Channel}", channel.Name);
            throw;
        }
    }

    private bool ShouldSendNotification(NotificationChannelConfig channel, HealthCheckResult result)
    {
        // Check if enough time has passed since last notification
        if (!HasEnoughTimePassed(channel.Name))
        {
            return false;
        }

        // Check if result status meets minimum severity
        if (!MeetsMinimumSeverity(channel.Filter.MinimumStatus, result.Status.ToString()))
        {
            return false;
        }

        // Check if any failing checks match included tags
        var failingEntries = result.Entries
            .Where(e => e.Value.Status != HealthStatus.Healthy)
            .Select(e => e.Key);

        return channel.Filter.IncludeTags.Any(tag => failingEntries.Contains(tag));
    }

    private bool HasEnoughTimePassed(string channelName)
    {
        lock (_lock)
        {
            if (!_lastNotifications.TryGetValue(channelName, out var lastNotification))
            {
                return true;
            }

            var minimumInterval = TimeSpan.FromSeconds(
                _config.HealthChecks.Notifications.MinimumSecondsBetweenNotifications);
            return DateTime.UtcNow - lastNotification > minimumInterval;
        }
    }

    private void UpdateLastNotificationTime(string channelName)
    {
        lock (_lock)
        {
            _lastNotifications[channelName] = DateTime.UtcNow;
        }
    }

    private bool MeetsMinimumSeverity(List<string> minimumStatus, string actualStatus)
    {
        if (!minimumStatus.Any())
        {
            return true;
        }

        return minimumStatus.Contains(actualStatus, StringComparer.OrdinalIgnoreCase);
    }

    private async Task SendSlackNotification(NotificationChannelConfig channel, HealthCheckResult result)
    {
        var webhookUrl = channel.Settings["WebhookUrl"];
        var payload = new
        {
            text = FormatSlackMessage(result),
            channel = channel.Settings.GetValueOrDefault("Channel", "#alerts"),
            username = channel.Settings.GetValueOrDefault("Username", "Health Check Monitor")
        };

        using var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsJsonAsync(webhookUrl, payload);
        response.EnsureSuccessStatusCode();
    }

    private async Task SendEmailNotification(NotificationChannelConfig channel, HealthCheckResult result)
    {
        // Implement email notification logic
        await Task.CompletedTask;
    }

    private async Task SendPagerDutyNotification(NotificationChannelConfig channel, HealthCheckResult result)
    {
        var serviceKey = channel.Settings["ServiceKey"];
        var payload = new
        {
            routing_key = serviceKey,
            event_action = "trigger",
            payload = new
            {
                summary = FormatPagerDutySummary(result),
                source = "TradingSystem",
                severity = MapStatusToPagerDutySeverity(result.Status.ToString()),
                timestamp = DateTime.UtcNow.ToString("O"),
                custom_details = result.Entries
                    .Where(e => e.Value.Status != HealthStatus.Healthy)
                    .ToDictionary(e => e.Key, e => e.Value.Description ?? "No description")
            }
        };

        using var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "https://events.pagerduty.com/v2/enqueue",
            payload);
        response.EnsureSuccessStatusCode();
    }

    private string FormatSlackMessage(HealthCheckResult result)
    {
        var status = result.Status == HealthStatus.Healthy ? ":white_check_mark:" : ":x:";
        var message = $"{status} Health Check Status: *{result.Status}*\n";
        message += $"Time: {DateTime.UtcNow:u}\n";

        foreach (var entry in result.Entries.Where(e => e.Value.Status != HealthStatus.Healthy))
        {
            message += $"• {entry.Key}: {entry.Value.Status}\n";
            if (!string.IsNullOrEmpty(entry.Value.Description))
            {
                message += $"  {entry.Value.Description}\n";
            }
        }

        return message;
    }

    private string FormatPagerDutySummary(HealthCheckResult result)
    {
        var failedChecks = result.Entries
            .Count(e => e.Value.Status != HealthStatus.Healthy);
        return $"Health Check Alert: {failedChecks} checks failing";
    }

    private string MapStatusToPagerDutySeverity(string status)
    {
        return status.ToLower() switch
        {
            "unhealthy" => "critical",
            "degraded" => "warning",
            _ => "info"
        };
    }
}
