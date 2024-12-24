using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using TradingSystem.Core.Configuration;
using TradingSystem.Common.Interfaces;
using TradingSystem.Common.Models;
using TradingSystem.Core.Services;
using TradingSystem.Infrastructure.Data;
using TradingSystem.Infrastructure.Repositories;
using TradingSystem.RealTrading.Services;
using TradingSystem.RealTrading.Configuration;
using TradingSystem.StrategySearch.Services;

namespace TradingSystem.Console.Services;

public class TradingSystemFactory
{
    public static IServiceProvider CreateTradingSystem(TradingEnvironmentConfig config)
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Register configuration
        services.AddSingleton(config);

        // Register database context
        services.AddDbContext<TradingContext>(options =>
        {
            options.UseNpgsql(config.Database.ConnectionString);
        });

        // Register repositories
        services.AddScoped<ITradeRepository, TradeRepository>();
        services.AddScoped<IMarketDataRepository, MarketDataRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();

        // Register validation service
        services.AddSingleton<IRiskValidationService, RiskValidationService>();

        // Register exchange adapter based on environment
        services.AddSingleton<IExchangeAdapter>(provider =>
        {
            if (config.Environment == TradingEnvironment.Test || config.Exchange?.Type == "Simulated")
            {
                return new SimulatedExchangeAdapter(
                    provider.GetRequiredService<ILogger<SimulatedExchangeAdapter>>());
            }

            throw new NotImplementedException("Only simulated trading is currently supported");
        });

        // Register market data service
        services.AddSingleton<IMarketDataService, MarketDataService>();

        // Register risk manager
        services.AddSingleton<IRiskManager>(provider =>
        {
            var riskMetrics = new RiskMetrics
            {
                RiskParameters = new Dictionary<string, decimal>
                {
                    ["MaxPositionSize"] = config.Risk.MaxPositionSize,
                    ["StopLossPercent"] = config.Risk.StopLossPercent,
                    ["MaxDrawdown"] = config.Risk.MaxDrawdown,
                    ["MaxDailyLoss"] = config.Risk.MaxDailyLoss,
                    ["MaxOpenPositions"] = config.Risk.MaxOpenPositions,
                    ["TakeProfitPercent"] = config.Risk.TakeProfitPercent
                }
            };

            var riskManagerConfig = new RiskManagerConfig
            {
                MaxPositionSize = config.Risk.MaxPositionSize,
                DefaultStopLossPercent = config.Risk.StopLossPercent,
                MaxDrawdown = config.Risk.MaxDrawdown,
                MaxRiskPerTrade = config.Risk.MaxDailyLoss,
                MaxPortfolioRisk = config.Risk.MaxPositionSize,
                MinPositionSize = 0.001m,
                DefaultTakeProfitPercent = config.Risk.TakeProfitPercent,
                MinRiskRewardRatio = 1.5m,
                MaxLeverage = 3m,
                InitialAccountEquity = 10000m
            };

            return new RiskManager(
                provider.GetRequiredService<ITradeRepository>(),
                provider.GetRequiredService<IRiskValidationService>(),
                Microsoft.Extensions.Options.Options.Create(riskManagerConfig),
                provider.GetRequiredService<ILogger<RiskManager>>());
        });

        // Register trading service
        services.AddSingleton<ITradingService, TradingService>();

        return services.BuildServiceProvider();
    }

    public static async Task InitializeSystem(IServiceProvider serviceProvider, TradingEnvironment environment)
    {
        // Initialize database
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TradingContext>();
        await dbContext.Database.MigrateAsync();

        // Get configuration
        var config = serviceProvider.GetRequiredService<TradingEnvironmentConfig>();

        // Initialize exchange adapter
        var exchangeAdapter = serviceProvider.GetRequiredService<IExchangeAdapter>();
        foreach (var symbol in config.Exchange?.Symbols ?? new[] { "BTCUSD" })
        {
            exchangeAdapter.SubscribeToSymbol(symbol);
        }

        // Verify exchange connection
        try
        {
            await exchangeAdapter.GetAccountBalance("USDT");
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to connect to exchange", ex);
        }
    }
}
