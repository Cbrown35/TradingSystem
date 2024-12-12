using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using TradingSystem.Common.Interfaces;

namespace TradingSystem.Core.Monitoring.HealthChecks;

public class StrategyExecutionHealthCheck : IHealthCheck
{
    private readonly ILogger<StrategyExecutionHealthCheck> _logger;
    private readonly ITradingService _tradingService;

    public StrategyExecutionHealthCheck(
        ILogger<StrategyExecutionHealthCheck> logger,
        ITradingService tradingService)
    {
        _logger = logger;
        _tradingService = tradingService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get active strategies and their status
            var activeStrategies = await _tradingService.GetActiveStrategies();
            var performanceMetrics = await _tradingService.GetStrategyPerformance();

            var data = new Dictionary<string, object>
            {
                { "LastCheckTime", DateTime.UtcNow },
                { "ActiveStrategies", activeStrategies.Count },
                { "TotalTrades", performanceMetrics.TotalTrades },
                { "WinRate", performanceMetrics.WinRate },
                { "ProfitLossRatio", performanceMetrics.ProfitLossRatio }
            };

            // Check if any strategies are in error state
            var failedStrategies = activeStrategies.Count(s => s.Status == "Error");
            if (failedStrategies > 0)
            {
                data.Add("FailedStrategies", failedStrategies);
                return HealthCheckResult.Degraded(
                    $"Strategy execution system has {failedStrategies} failed strategies",
                    null,
                    data);
            }

            // Check if performance metrics indicate issues
            if (performanceMetrics.ErrorRate > 0.05m) // 5% error threshold
            {
                data.Add("ErrorRate", performanceMetrics.ErrorRate);
                return HealthCheckResult.Degraded(
                    "Strategy execution system has elevated error rate",
                    null,
                    data);
            }

            return HealthCheckResult.Healthy(
                "Strategy execution system is operational",
                data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Strategy execution health check failed");
            return HealthCheckResult.Unhealthy(
                "Strategy execution system check failed",
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
