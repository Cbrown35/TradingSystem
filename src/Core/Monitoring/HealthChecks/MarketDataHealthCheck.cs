using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using TradingSystem.Common.Interfaces;

namespace TradingSystem.Core.Monitoring.HealthChecks;

public class MarketDataHealthCheck : IHealthCheck
{
    private readonly IMarketDataService _marketDataService;
    private readonly ILogger<MarketDataHealthCheck> _logger;
    private readonly string[] _monitoredSymbols = { "BTCUSD", "ETHUSD", "ES", "NQ" };

    public MarketDataHealthCheck(
        IMarketDataService marketDataService,
        ILogger<MarketDataHealthCheck> logger)
    {
        _marketDataService = marketDataService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var activeSymbols = await _marketDataService.GetActiveSymbolsAsync();
            if (!activeSymbols.Any())
            {
                return HealthCheckResult.Degraded("No active symbols found");
            }

            var data = new Dictionary<string, object>();
            var errors = new List<string>();

            // Check market data for monitored symbols
            foreach (var symbol in _monitoredSymbols)
            {
                try
                {
                    var marketData = await _marketDataService.GetLatestMarketDataAsync(symbol);
                    if (marketData == null)
                    {
                        errors.Add($"No market data available for {symbol}");
                        continue;
                    }

                    // Add basic metrics for each symbol
                    data[$"{symbol}_LastUpdate"] = marketData.Timestamp;
                    data[$"{symbol}_Price"] = marketData.Close;
                    data[$"{symbol}_Volume"] = marketData.Volume;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking market data for {Symbol}", symbol);
                    errors.Add($"Error checking {symbol}: {ex.Message}");
                }
            }

            if (errors.Any())
            {
                return HealthCheckResult.Degraded(
                    $"Market data service partially operational. Errors: {string.Join(", ", errors)}",
                    data: data);
            }

            return HealthCheckResult.Healthy("Market data service is operational", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Market data health check failed");
            return HealthCheckResult.Unhealthy("Market data health check failed", ex);
        }
    }
}
