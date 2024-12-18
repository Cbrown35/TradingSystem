using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Core.Configuration;
using TradingSystem.Core.Monitoring.Models;

namespace TradingSystem.Core.Monitoring.Services;

public class HealthCheckStorageService : IHealthCheckStorageService
{
    private readonly ILogger<HealthCheckStorageService> _logger;
    private readonly TradingSystem.Core.Configuration.MonitoringConfig _config;
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, List<HealthReport>> _history;
    private readonly TimeSpan _retentionPeriod = TimeSpan.FromDays(7);

    public HealthCheckStorageService(
        ILogger<HealthCheckStorageService> logger,
        IOptions<TradingSystem.Core.Configuration.MonitoringConfig> config,
        IMemoryCache cache)
    {
        _logger = logger;
        _config = config.Value;
        _cache = cache;
        _history = new ConcurrentDictionary<string, List<HealthReport>>();
    }

    public async Task StoreHealthCheckResultAsync(HealthReport result)
    {
        try
        {
            var key = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var history = _history.GetOrAdd(key, _ => new List<HealthReport>());

            lock (history)
            {
                history.Add(result);

                // Keep only the last N entries per day
                var maxEntries = _config.HealthChecks.UI.ShowDetailedErrors ? 100 : 50;
                while (history.Count > maxEntries)
                {
                    history.RemoveAt(0);
                }
            }

            if (_config.HealthChecks.Logging.StoreInDatabase)
            {
                await StoreInDatabaseAsync(result);
            }

            _logger.LogDebug("Stored health check result");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing health check result");
            throw;
        }
    }

    public async Task<IEnumerable<HealthReport>> GetHealthCheckHistoryAsync(
        DateTime startTime,
        DateTime endTime)
    {
        try
        {
            var results = new List<HealthReport>();
            var dates = GetDateRange(startTime, endTime);

            foreach (var date in dates)
            {
                var key = date.ToString("yyyy-MM-dd");
                if (_history.TryGetValue(key, out var dayHistory))
                {
                    lock (dayHistory)
                    {
                        results.AddRange(dayHistory);
                    }
                }
            }

            return results.OrderByDescending(r => r.TotalDuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving health check history");
            throw;
        }
    }

    public async Task<HealthCheckAnalytics> GetAnalytics(string endpoint, DateTime startTime, DateTime endTime)
    {
        try
        {
            var reports = await GetHealthCheckHistoryAsync(startTime, endTime);
            var analytics = new HealthCheckAnalytics
            {
                StartTime = startTime,
                EndTime = endTime,
                TotalChecks = reports.Count(),
                HealthyChecks = reports.Count(r => r.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy),
                UnhealthyChecks = reports.Count(r => r.Status != Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy),
                AverageDuration = reports.Any() ? 
                    reports.Average(r => r.TotalDuration.TotalMilliseconds) : 
                    0
            };

            // Group issues by description
            var issues = reports
                .Where(r => r.Status != Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy)
                .SelectMany(r => r.Entries)
                .Where(e => e.Value.Status != Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy)
                .GroupBy(e => e.Value.Description)
                .Select(g => new HealthCheckIssue
                {
                    Name = g.First().Key,
                    Description = g.First().Value.Description ?? "",
                    Status = g.First().Value.Status.ToString(),
                    Count = g.Count(),
                    LastOccurrence = DateTime.UtcNow // Using current time as reports don't have timestamps
                })
                .OrderByDescending(i => i.Count)
                .ToList();

            analytics.Issues = issues;
            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting health check analytics");
            throw;
        }
    }

    public async Task CleanupOldEntriesAsync()
    {
        try
        {
            var cutoffDate = DateTime.UtcNow - _retentionPeriod;
            var oldKeys = _history.Keys
                .Where(k => DateTime.TryParse(k, out var date) && date < cutoffDate)
                .ToList();

            foreach (var key in oldKeys)
            {
                _history.TryRemove(key, out _);
            }

            if (_config.HealthChecks.Logging.StoreInDatabase)
            {
                await CleanupDatabaseAsync(cutoffDate);
            }

            _logger.LogInformation("Cleaned up {Count} old health check entries", oldKeys.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old health check entries");
            throw;
        }
    }

    private IEnumerable<DateTime> GetDateRange(DateTime startTime, DateTime endTime)
    {
        for (var date = startTime.Date; date <= endTime.Date; date = date.AddDays(1))
        {
            yield return date;
        }
    }

    private async Task StoreInDatabaseAsync(HealthReport result)
    {
        // Implement database storage if needed
        await Task.CompletedTask;
    }

    private async Task CleanupDatabaseAsync(DateTime cutoffDate)
    {
        // Implement database cleanup if needed
        await Task.CompletedTask;
    }
}
