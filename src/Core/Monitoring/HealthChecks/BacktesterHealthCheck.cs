using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using TradingSystem.Common.Interfaces;
using TradingSystem.Common.Models;

namespace TradingSystem.Core.Monitoring.HealthChecks;

public class BacktesterHealthCheck : IHealthCheck
{
    private readonly ILogger<BacktesterHealthCheck> _logger;
    private readonly IBacktester _backtester;
    private readonly ITheoryGenerator _theoryGenerator;

    public BacktesterHealthCheck(
        ILogger<BacktesterHealthCheck> logger,
        IBacktester backtester,
        ITheoryGenerator theoryGenerator)
    {
        _logger = logger;
        _backtester = backtester;
        _theoryGenerator = theoryGenerator;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Generate a simple test theory
            var testTheory = await _theoryGenerator.GenerateTheory(new[] { "BTC-USD" });
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;

            // Run a quick backtest to verify system
            var result = await _backtester.RunBacktest(testTheory, startDate, endDate);

            var data = new Dictionary<string, object>
            {
                { "LastCheckTime", DateTime.UtcNow },
                { "TestPeriod", $"{startDate:d} to {endDate:d}" },
                { "TotalTrades", result.TotalTrades },
                { "BacktestDuration", result.EndDate - result.StartDate },
                { "SystemStatus", "Operational" }
            };

            return HealthCheckResult.Healthy(
                "Backtesting system is operational",
                data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backtester health check failed");
            return HealthCheckResult.Unhealthy(
                "Backtesting system is not responding",
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
