using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Core.Configuration;
using TradingSystem.Core.Monitoring.Interfaces;

namespace TradingSystem.Core.Monitoring;

public class HealthCheckService : IHealthCheckService
{
    private readonly ILogger<HealthCheckService> _logger;
    private readonly TradingSystem.Core.Configuration.MonitoringConfig _config;
    private readonly IServiceProvider _serviceProvider;
    private HealthReport? _latestReport;

    public HealthCheckService(
        ILogger<HealthCheckService> logger,
        IOptions<TradingSystem.Core.Configuration.MonitoringConfig> config,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _config = config.Value;
        _serviceProvider = serviceProvider;
    }

    public async Task<HealthReport> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var data = new Dictionary<string, object>
            {
                { "LastCheckTime", DateTime.UtcNow },
                { "Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production" }
            };

            // Check core services
            var coreServicesHealthy = await CheckCoreServicesAsync();
            if (!coreServicesHealthy)
            {
                _latestReport = new HealthReport(
                    new Dictionary<string, HealthReportEntry>
                    {
                        { "CoreServices", new HealthReportEntry(
                            HealthStatus.Unhealthy,
                            "Core services check failed",
                            TimeSpan.Zero,
                            null,
                            data) }
                    },
                    TimeSpan.Zero);
                return _latestReport;
            }

            // Check infrastructure
            var infrastructureHealthy = await CheckInfrastructureAsync();
            if (!infrastructureHealthy)
            {
                _latestReport = new HealthReport(
                    new Dictionary<string, HealthReportEntry>
                    {
                        { "Infrastructure", new HealthReportEntry(
                            HealthStatus.Degraded,
                            "Infrastructure check failed",
                            TimeSpan.Zero,
                            null,
                            data) }
                    },
                    TimeSpan.Zero);
                return _latestReport;
            }

            // Check dependencies
            var dependenciesHealthy = await CheckDependenciesAsync();
            if (!dependenciesHealthy)
            {
                _latestReport = new HealthReport(
                    new Dictionary<string, HealthReportEntry>
                    {
                        { "Dependencies", new HealthReportEntry(
                            HealthStatus.Degraded,
                            "Dependencies check failed",
                            TimeSpan.Zero,
                            null,
                            data) }
                    },
                    TimeSpan.Zero);
                return _latestReport;
            }

            _latestReport = new HealthReport(
                new Dictionary<string, HealthReportEntry>
                {
                    { "Overall", new HealthReportEntry(
                        HealthStatus.Healthy,
                        "All systems operational",
                        TimeSpan.Zero,
                        null,
                        data) }
                },
                TimeSpan.Zero);
            return _latestReport;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing health check");
            _latestReport = new HealthReport(
                new Dictionary<string, HealthReportEntry>
                {
                    { "Error", new HealthReportEntry(
                        HealthStatus.Unhealthy,
                        "Health check failed",
                        TimeSpan.Zero,
                        ex,
                        null) }
                },
                TimeSpan.Zero);
            return _latestReport;
        }
    }

    public Task<HealthReport> GetLatestHealthReportAsync()
    {
        return Task.FromResult(_latestReport ?? new HealthReport(
            new Dictionary<string, HealthReportEntry>(),
            TimeSpan.Zero));
    }

    public Task<IEnumerable<HealthReport>> GetHistoricalHealthReportsAsync(DateTime startTime, DateTime endTime)
    {
        // In a real implementation, this would fetch from storage
        return Task.FromResult<IEnumerable<HealthReport>>(new[] { _latestReport ?? new HealthReport(
            new Dictionary<string, HealthReportEntry>(),
            TimeSpan.Zero) });
    }

    public HealthStatus GetComponentStatus(string componentName)
    {
        if (_latestReport?.Entries.TryGetValue(componentName, out var entry) == true)
        {
            return entry.Status;
        }
        return HealthStatus.Unhealthy; // Default to unhealthy instead of Unknown
    }

    public IReadOnlyDictionary<string, HealthStatus> GetAllComponentStatuses()
    {
        if (_latestReport == null)
        {
            return new Dictionary<string, HealthStatus>();
        }

        return _latestReport.Entries.ToDictionary(
            e => e.Key,
            e => e.Value.Status);
    }

    private async Task<bool> CheckCoreServicesAsync()
    {
        try
        {
            // Add core service checks here
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Core services check failed");
            return false;
        }
    }

    private async Task<bool> CheckInfrastructureAsync()
    {
        try
        {
            // Add infrastructure checks here
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Infrastructure check failed");
            return false;
        }
    }

    private async Task<bool> CheckDependenciesAsync()
    {
        try
        {
            // Add dependency checks here
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dependencies check failed");
            return false;
        }
    }
}
