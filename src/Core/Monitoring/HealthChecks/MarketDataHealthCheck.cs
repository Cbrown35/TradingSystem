using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using TradingSystem.Common.Interfaces;

namespace TradingSystem.Core.Monitoring.HealthChecks;

public class MarketDataHealthCheck : IHealthCheck
{
    private readonly ILogger<MarketDataHealthCheck> _logger;
    private readonly IMarketDataService _marketDataService;

    public MarketDataHealthCheck(
        ILogger<MarketDataHealthCheck> logger,
        IMarketDataService marketDataService)
    {
        _logger = logger;
        _marketDataService = marketDataService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test market data service by getting a sample price
            var testSymbol = "BTC-USD"; // Example test symbol
            var marketData = await _marketDataService.GetMarketData(testSymbol);

            var data = new Dictionary<string, object>
            {
                { "LastCheckTime", DateTime.UtcNow },
                { "TestSymbol", testSymbol },
                { "LastPrice", marketData.LastPrice },
                { "DataFreshness", DateTime.UtcNow - marketData.Timestamp }
            };

            return HealthCheckResult.Healthy(
                "Market data services are operational",
                data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Market data health check failed");
            return HealthCheckResult.Unhealthy(
                "Market data services are not responding",
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
