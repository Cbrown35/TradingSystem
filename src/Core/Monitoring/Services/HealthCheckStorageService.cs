using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Core.Configuration;
using TradingSystem.Core.Monitoring.Models;

namespace TradingSystem.Core.Monitoring.Services;

public class HealthCheckStorageService
{
    private readonly ILogger<HealthCheckStorageService> _logger;
    private readonly MonitoringConfig _config;
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, List<HealthCheckResult>> _history;

    public HealthCheckStorageService(
        ILogger<HealthCheckStorageService> logger,
        IOptions<MonitoringConfig> config,
        IMemoryCache cache)
    {
        _logger = logger;
        _config = config.Value;
        _cache = cache;
        _history = new ConcurrentDictionary<string, List<HealthCheckResult>>();
    }

    public async Task StoreResult(string endpoint, HealthCheckResult result)
    {
        try
        {
            var history = _history.GetOrAdd(endpoint, _ => new List<HealthCheckResult>());

            lock (history)
            {
                history.Add(result);

                // Keep only the last N entries
                var maxEntries = _config.HealthChecks.UI.ShowDetailedErrors ? 100 : 50;
                while (history.Count > maxEntries)
                {
                    history.RemoveAt(0);
                }
            }

            if (_config.HealthChecks.Logging.StoreInDatabase)
            {
                await StoreInDatabase(endpoint, result);
            }

            _logger.LogDebug("Stored health check result for endpoint {Endpoint}", endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing health check result for endpoint {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<IEnumerable<HealthCheckResult>> GetHistory(
        string endpoint,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? limit = null)
    {
        try
        {
            if (!_history.TryGetValue(endpoint, out var history))
            {
                return Enumerable.Empty<HealthCheckResult>();
            }

            IEnumerable<HealthCheckResult> results;
            lock (history)
            {
                results = history.ToList();
            }

            if (startTime.HasValue)
            {
                results = results.Where(r => r.Entries.Any(e => e.Value.Data.ContainsKey("LastCheckTime") && 
                    (DateTime)e.Value.Data["LastCheckTime"] >= startTime.Value));
            }

            if (endTime.HasValue)
            {
                results = results.Where(r => r.Entries.Any(e => e.Value.Data.ContainsKey("LastCheckTime") && 
                    (DateTime)e.Value.Data["LastCheckTime"] <= endTime.Value));
            }

            if (limit.HasValue)
            {
                results = results.TakeLast(limit.Value);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving health check history for endpoint {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<HealthCheckAnalytics> GetAnalytics(
        string endpoint,
        DateTime startTime,
        DateTime endTime)
    {
        try
        {
            var history = await GetHistory(endpoint, startTime, endTime);
            var analytics = new HealthCheckAnalytics
            {
                StartTime = startTime,
                EndTime = endTime,
                TotalChecks = history.Count(),
                HealthyChecks = history.Count(r => r.Status == HealthStatus.Healthy),
                UnhealthyChecks = history.Count(r => r.Status != HealthStatus.Healthy),
                AverageDuration = history.Average(r => r.TotalDuration.TotalMilliseconds),
                Issues = GetTopIssues(history)
            };

            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating health check analytics for endpoint {Endpoint}", endpoint);
            throw;
        }
    }

    private List<HealthCheckIssue> GetTopIssues(IEnumerable<HealthCheckResult> history)
    {
        return history
            .Where(r => r.Status != HealthStatus.Healthy)
            .SelectMany(r => r.Entries)
            .Where(e => e.Value.Status != HealthStatus.Healthy)
            .GroupBy(e => new { Name = e.Key, Status = e.Value.Status, Description = e.Value.Description })
            .Select(g => new HealthCheckIssue
            {
                Name = g.Key.Name,
                Status = g.Key.Status.ToString(),
                Description = g.Key.Description ?? "No description",
                Count = g.Count(),
                LastOccurrence = g.Max(e => e.Value.Data.ContainsKey("LastCheckTime") 
                    ? (DateTime)e.Value.Data["LastCheckTime"] 
                    : DateTime.UtcNow)
            })
            .OrderByDescending(i => i.Count)
            .ToList();
    }

    private async Task StoreInDatabase(string endpoint, HealthCheckResult result)
    {
        // Implement database storage if needed
        await Task.CompletedTask;
    }
}

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
