using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TradingSystem.Core.Monitoring.HealthChecks;

namespace TradingSystem.Core.Monitoring;

public static class ComponentHealthChecks
{
    public static IServiceCollection AddTradingSystemHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<BacktesterHealthCheck>("backtester", tags: new[] { "trading" })
            .AddCheck<MarketDataHealthCheck>("market_data", tags: new[] { "trading", "data" })
            .AddCheck<RiskManagementHealthCheck>("risk_management", tags: new[] { "trading", "risk" })
            .AddCheck<StrategyExecutionHealthCheck>("strategy_execution", tags: new[] { "trading", "strategy" });

        return services;
    }
}
