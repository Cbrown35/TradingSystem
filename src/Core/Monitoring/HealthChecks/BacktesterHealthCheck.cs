using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using TradingSystem.Common.Interfaces;
using TradingSystem.Common.Models;
using TradingSystem.Core.Monitoring.Extensions;

namespace TradingSystem.Core.Monitoring.HealthChecks;

public class BacktesterHealthCheck : IHealthCheck
{
    private readonly ILogger<BacktesterHealthCheck> _logger;
    private readonly IBacktester _backtester;
    private readonly TimeSpan _maxBacktestDuration = TimeSpan.FromMinutes(5);

    public BacktesterHealthCheck(
        ILogger<BacktesterHealthCheck> logger,
        IBacktester backtester)
    {
        _logger = logger;
        _backtester = backtester;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Run a simple backtest to verify functionality
            var testStrategy = new SimpleBacktestStrategy();
            var startTime = DateTime.UtcNow.AddDays(-1);
            var endTime = DateTime.UtcNow;
            var symbol = "TEST";

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = await _backtester.RunBacktestAsync(testStrategy, symbol, startTime, endTime);
            sw.Stop();

            var issues = new List<string>();

            // Check backtest duration
            if (sw.Elapsed > _maxBacktestDuration)
            {
                issues.Add($"Backtest took too long: {sw.Elapsed.TotalSeconds:N1} seconds");
            }

            // Check for data gaps
            var dataGaps = result.GetDataGaps();
            if (dataGaps.Any())
            {
                issues.Add($"Found {dataGaps.Count} data gaps in backtest period");
            }

            // Check for processing errors
            var processingErrors = result.GetProcessingErrors();
            if (processingErrors.Any())
            {
                issues.Add($"Encountered {processingErrors.Count} processing errors");
            }

            // Check memory usage
            var memoryUsage = result.GetMemoryUsage();
            if (memoryUsage > 1024 * 1024 * 512) // 512MB threshold
            {
                issues.Add($"High memory usage: {memoryUsage / (1024 * 1024)}MB");
            }

            if (issues.Any())
            {
                var data = new Dictionary<string, object>
                {
                    ["LastCheckTime"] = DateTime.UtcNow,
                    ["BacktestDuration"] = sw.Elapsed,
                    ["DataGaps"] = dataGaps.Count,
                    ["ProcessingErrors"] = processingErrors.Count,
                    ["MemoryUsage"] = memoryUsage,
                    ["Issues"] = issues
                };

                return processingErrors.Count > 5 || sw.Elapsed > _maxBacktestDuration * 2
                    ? HealthCheckResult.Unhealthy("Critical backtesting issues detected", data: data)
                    : HealthCheckResult.Degraded("Backtesting performance issues detected", data: data);
            }

            return HealthCheckResult.Healthy(
                "Backtesting system operating normally",
                data: new Dictionary<string, object>
                {
                    ["LastCheckTime"] = DateTime.UtcNow,
                    ["BacktestDuration"] = sw.Elapsed,
                    ["MemoryUsage"] = memoryUsage
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking backtester health");
            return HealthCheckResult.Unhealthy(
                "Failed to check backtester health",
                ex,
                data: new Dictionary<string, object>
                {
                    ["LastCheckTime"] = DateTime.UtcNow,
                    ["ErrorType"] = ex.GetType().Name,
                    ["ErrorMessage"] = ex.Message
                });
        }
    }

    private class SimpleBacktestStrategy : ITradingStrategy
    {
        private readonly Dictionary<string, decimal> _parameters = new()
        {
            ["ShortPeriod"] = 10,
            ["LongPeriod"] = 20
        };

        public string Id => $"HealthCheck_{Name}_{Guid.NewGuid():N}";
        public string Name => "HealthCheckTestStrategy";

        public Task<Signal> GenerateSignalAsync(MarketData marketData)
        {
            // Simple moving average crossover for testing
            return Task.FromResult(new Signal
            {
                Name = $"Test Signal for {marketData.Symbol}",
                Type = SignalType.Entry,
                Timestamp = DateTime.UtcNow,
                Parameters = new Dictionary<string, decimal>
                {
                    ["Price"] = marketData.Close,
                    ["Volume"] = marketData.Volume
                }
            });
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public Task UpdateAsync(MarketData marketData)
        {
            return Task.CompletedTask;
        }

        public Task<bool?> ShouldEnter(List<MarketData> marketData)
        {
            // Simple test logic
            return Task.FromResult<bool?>(marketData.Count >= _parameters["LongPeriod"]);
        }

        public Task<bool?> ShouldExit(List<MarketData> marketData)
        {
            // Simple test logic
            return Task.FromResult<bool?>(marketData.Count >= _parameters["ShortPeriod"]);
        }

        public Task<Dictionary<string, decimal>> GetParameters()
        {
            return Task.FromResult(new Dictionary<string, decimal>(_parameters));
        }

        public Task SetParameters(Dictionary<string, decimal> parameters)
        {
            foreach (var param in parameters)
            {
                if (_parameters.ContainsKey(param.Key))
                {
                    _parameters[param.Key] = param.Value;
                }
            }
            return Task.CompletedTask;
        }
    }
}
