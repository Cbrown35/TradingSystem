using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using TradingSystem.Common.Interfaces;
using TradingSystem.Common.Models;

namespace TradingSystem.Core.Monitoring.HealthChecks;

public class RiskManagementHealthCheck : IHealthCheck
{
    private readonly IRiskManager _riskManager;
    private readonly ILogger<RiskManagementHealthCheck> _logger;

    public RiskManagementHealthCheck(
        IRiskManager riskManager,
        ILogger<RiskManagementHealthCheck> logger)
    {
        _riskManager = riskManager;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var metrics = await _riskManager.GetCurrentRiskMetricsAsync();
            var riskLimits = await _riskManager.GetRiskLimitsAsync();

            var data = new Dictionary<string, object>
            {
                { "AccountEquity", metrics.AccountEquity },
                { "TotalRisk", metrics.TotalRisk },
                { "MarginUsed", metrics.MarginUsed },
                { "MaxDrawdown", metrics.MaxDrawdown },
                { "OpenPositions", metrics.OpenPositions.Count }
            };

            // Add risk limits
            foreach (var limit in riskLimits)
            {
                data[$"Limit_{limit.Key}"] = limit.Value;
            }

            // Check for risk threshold violations
            var warnings = new List<string>();

            // Check portfolio risk
            var portfolioRisk = metrics.TotalRisk / metrics.AccountEquity;
            if (portfolioRisk > riskLimits["MaxPortfolioRisk"])
            {
                warnings.Add($"Portfolio risk ({portfolioRisk:P2}) exceeds maximum allowed ({riskLimits["MaxPortfolioRisk"]:P2})");
            }

            // Check drawdown
            if (metrics.MaxDrawdown > riskLimits["MaxDrawdown"])
            {
                warnings.Add($"Drawdown ({metrics.MaxDrawdown:P2}) exceeds maximum allowed ({riskLimits["MaxDrawdown"]:P2})");
            }

            // Check margin usage
            var marginUsageRatio = metrics.MarginUsed / metrics.AccountEquity;
            if (marginUsageRatio > riskLimits["MaxLeverage"])
            {
                warnings.Add($"Margin usage ({marginUsageRatio:P2}) exceeds maximum leverage ({riskLimits["MaxLeverage"]:P2})");
            }

            if (warnings.Any())
            {
                return HealthCheckResult.Degraded(
                    $"Risk management warnings: {string.Join(", ", warnings)}",
                    data: data);
            }

            return HealthCheckResult.Healthy("Risk management system is operational", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Risk management health check failed");
            return HealthCheckResult.Unhealthy("Risk management health check failed", ex);
        }
    }
}
