using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TradingSystem.Core.Monitoring.Interfaces;

public interface IHealthCheckService
{
    Task<HealthReport> CheckHealthAsync(CancellationToken cancellationToken = default);
    Task<HealthReport> GetLatestHealthReportAsync();
    Task<IEnumerable<HealthReport>> GetHistoricalHealthReportsAsync(DateTime startTime, DateTime endTime);
    HealthStatus GetComponentStatus(string componentName);
    IReadOnlyDictionary<string, HealthStatus> GetAllComponentStatuses();
}
