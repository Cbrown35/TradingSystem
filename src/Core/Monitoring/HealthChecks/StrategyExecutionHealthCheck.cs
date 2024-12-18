using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using TradingSystem.Common.Interfaces;
using TradingSystem.Common.Models;

namespace TradingSystem.Core.Monitoring.HealthChecks;

public class StrategyExecutionHealthCheck : IHealthCheck
{
    private readonly ITradingService _tradingService;
    private readonly IMarketDataService _marketDataService;
    private readonly ILogger<StrategyExecutionHealthCheck> _logger;

    public StrategyExecutionHealthCheck(
        ITradingService tradingService,
        IMarketDataService marketDataService,
        ILogger<StrategyExecutionHealthCheck> logger)
    {
        _tradingService = tradingService;
        _marketDataService = marketDataService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = new Dictionary<string, object>();
            var warnings = new List<string>();

            // Check active strategies
            var activeStrategies = await _tradingService.GetActiveStrategiesAsync();
            data["ActiveStrategies"] = activeStrategies.Count();

            if (!activeStrategies.Any())
            {
                warnings.Add("No active trading strategies found");
            }

            // Check strategy execution status
            foreach (var strategy in activeStrategies)
            {
                try
                {
                    var performance = await _tradingService.GetStrategyPerformanceAsync(strategy.Id);
                    var metrics = await _tradingService.GetStrategyMetricsAsync(strategy.Id);
                    
                    data[$"Strategy_{strategy.Name}_Status"] = performance.IsExecuting ? "Running" : "Stopped";
                    data[$"Strategy_{strategy.Name}_ErrorRate"] = performance.ErrorRate;
                    data[$"Strategy_{strategy.Name}_Latency"] = performance.AverageLatency.TotalMilliseconds;
                    data[$"Strategy_{strategy.Name}_WinRate"] = performance.WinRate;
                    data[$"Strategy_{strategy.Name}_ProfitFactor"] = performance.ProfitFactor;

                    // Get recent trades
                    var recentTrades = await _tradingService.GetStrategyTradesAsync(
                        strategy.Id, 
                        DateTime.UtcNow.AddHours(-24));
                    data[$"Strategy_{strategy.Name}_RecentTrades"] = recentTrades.Count();

                    // Check for strategy-specific issues
                    if (!performance.IsExecuting)
                    {
                        warnings.Add($"Strategy {strategy.Name} is not executing");
                    }

                    if (performance.ErrorRate > 0.05m) // 5% error rate threshold
                    {
                        warnings.Add($"Strategy {strategy.Name} has high error rate: {performance.ErrorRate:P2}");
                    }

                    if (performance.AverageLatency > TimeSpan.FromSeconds(1))
                    {
                        warnings.Add($"Strategy {strategy.Name} has high latency: {performance.AverageLatency.TotalMilliseconds:F1}ms");
                    }

                    // Check market data for each symbol the strategy trades
                    var trades = await _tradingService.GetStrategyTradesAsync(strategy.Id, DateTime.UtcNow.AddDays(-1));
                    var symbols = trades.Select(t => t.Symbol).Distinct();

                    foreach (var symbol in symbols)
                    {
                        var marketData = await _marketDataService.GetLatestMarketDataAsync(symbol);
                        if (marketData == null)
                        {
                            warnings.Add($"Strategy {strategy.Name} is not receiving market data for {symbol}");
                        }
                        else
                        {
                            var dataAge = DateTime.UtcNow - marketData.Timestamp;
                            if (dataAge > TimeSpan.FromMinutes(5))
                            {
                                warnings.Add($"Market data for {symbol} is stale (Age: {dataAge.TotalMinutes:F1} minutes)");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking strategy {StrategyName}", strategy.Name);
                    warnings.Add($"Error checking strategy {strategy.Name}: {ex.Message}");
                }
            }

            if (warnings.Any())
            {
                return HealthCheckResult.Degraded(
                    $"Strategy execution warnings: {string.Join(", ", warnings)}",
                    data: data);
            }

            return HealthCheckResult.Healthy("Strategy execution system is operational", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Strategy execution health check failed");
            return HealthCheckResult.Unhealthy("Strategy execution health check failed", ex);
        }
    }
}
