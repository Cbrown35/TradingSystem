using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using TradingSystem.Common.Interfaces;

namespace TradingSystem.Core.Monitoring.HealthChecks;

public class RiskManagementHealthCheck : IHealthCheck
{
    private readonly ILogger<RiskManagementHealthCheck> _logger;
    private readonly IRiskManager _riskManager;

    public RiskManagementHealthCheck(
        ILogger<RiskManagementHealthCheck> logger,
        IRiskManager riskManager)
    {
        _logger = logger;
        _riskManager = riskManager;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test risk management service by getting current risk metrics
            var metrics = await _riskManager.GetCurrentRiskMetrics();

            var data = new Dictionary<string, object>
            {
                { "LastCheckTime", DateTime.UtcNow },
                { "TotalExposure", metrics.TotalExposure },
                { "MaxDrawdown", metrics.MaxDrawdown },
                { "PositionCount", metrics.OpenPositions },
                { "RiskLevel", metrics.CurrentRiskLevel }
            };

            // Check if any risk thresholds are exceeded
            if (metrics.CurrentRiskLevel > metrics.MaxAllowedRiskLevel)
            {
                return HealthCheckResult.Degraded(
                    "Risk management system reports elevated risk levels",
                    null,
                    data);
            }

            return HealthCheckResult.Healthy(
                "Risk management services are operational",
                data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Risk management health check failed");
            return HealthCheckResult.Unhealthy(
                "Risk management services are not responding",
                ex,
                new Dictionary<string, object>
                {
                    { "LastCheckTime", DateTime.UtcNow },
                    { "ErrorType", ex.GetType().Name },
                    { "ErrorMessage", ex.Message }
                });
        }
    }
}
