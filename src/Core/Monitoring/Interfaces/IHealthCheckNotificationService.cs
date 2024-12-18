using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TradingSystem.Core.Monitoring.Interfaces;

public interface IHealthCheckNotificationService
{
    Task ProcessHealthCheckResultAsync(HealthReport report);
    Task SendNotificationAsync(string componentName, HealthReportEntry entry);
}
