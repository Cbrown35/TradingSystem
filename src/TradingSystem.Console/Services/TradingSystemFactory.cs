using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using TradingSystem.Core.Configuration;
using TradingSystem.Core.Interfaces;
using TradingSystem.Core.Services;
using TradingSystem.Infrastructure.Data;
using TradingSystem.Infrastructure.Repositories;
using TradingSystem.RealTrading.Services;
using TradingSystem.StrategySearch.Services;
using TradingSystem.StrategySearch.Models;

namespace TradingSystem.Console.Services;

public class TradingSystemFactory
{
    public static IServiceProvider CreateTradingSystem(TradingEnvironment environment)
    {
        var services = new ServiceCollection();
        var config = EnvironmentConfigFactory.CreateConfig(environment);

        // Register configuration
        services.AddSingleton(config);

        // Register database context
        services.AddDbContext<TradingContext>(options =>
            options.UseSqlServer(config.Database.ConnectionString));

        // Register repositories
        services.AddScoped<ITradeRepository, TradeRepository>();

        // Register exchange adapter based on environment
        services.AddSingleton<IExchangeAdapter>(provider =>
        {
            if (config.Environment == TradingEnvironment.Test)
            {
                return new SimulatedExchangeAdapter();
            }
            else
            {
                var tradovateAdapter = new TradovateAdapter(config.Tradovate);
                if (config.Environment == TradingEnvironment.Development)
                {
                    // Wrap with TradingView adapter in dev for testing webhooks
                    return new TradingViewAdapter(config.TradingView, tradovateAdapter);
                }
                return tradovateAdapter;
            }
        });

        // Register market data service
        services.AddSingleton<IMarketDataService, MarketDataService>();

        // Register risk manager with environment-specific settings
        services.AddSingleton<IRiskManager>(provider =>
        {
            var riskParams = new RiskParameters
            {
                MaxPositionSize = config.Risk.MaxPositionSize,
                StopLossPercent = config.Risk.StopLossPercent,
                MaxDrawdown = config.Risk.MaxDrawdown,
                MaxDailyLoss = config.Risk.MaxDailyLoss,
                MaxOpenPositions = config.Risk.MaxOpenPositions,
                TakeProfitPercent = config.Risk.MaxPositionSize * 2 // Example: 2:1 reward/risk ratio
            };

            return new RiskManager(
                provider.GetRequiredService<IExchangeAdapter>(),
                provider.GetRequiredService<ITradeRepository>(),
                riskParams);
        });

        // Register strategy search components
        services.AddSingleton<ITheoryGenerator, TheoryGenerator>();
        services.AddSingleton<IBacktester, Backtester>();
        services.AddSingleton<IStrategyOptimizer, StrategyOptimizer>();
        services.AddSingleton<StrategySearchService>();

        // Register trading service
        services.AddSingleton<ITradingService, TradingService>();

        // Register system coordinator
        services.AddSingleton<TradingSystemCoordinator>();

        // Register Pine Script generator
        services.AddSingleton<PineScriptGenerator>();

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

        // Initialize services based on environment
        switch (environment)
        {
            case TradingEnvironment.Test:
                // No additional initialization needed for test environment
                break;

            case TradingEnvironment.Development:
                // Start TradingView webhook server
                var tradingViewAdapter = serviceProvider.GetRequiredService<IExchangeAdapter>() as TradingViewAdapter;
                // Webhook server is started automatically in the adapter
                break;

            case TradingEnvironment.Production:
                // Verify exchange connection
                var exchangeAdapter = serviceProvider.GetRequiredService<IExchangeAdapter>();
                try
                {
                    await exchangeAdapter.GetAccountBalance("USDT");
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to connect to exchange in production", ex);
                }
                break;
        }

        // Initialize trading coordinator
        var coordinator = serviceProvider.GetRequiredService<TradingSystemCoordinator>();
        // Additional coordinator initialization if needed
    }

    public static string GetEnvironmentSummary(TradingEnvironmentConfig config)
    {
        return $@"Trading System Environment: {config.Environment}

Database:
- Connection: {config.Database.ConnectionString}

Trading Configuration:
- Exchange: {(config.Tradovate.IsPaperTrading ? "Paper Trading" : "Live Trading")}
- Base URL: {config.Tradovate.BaseUrl}

Risk Parameters:
- Max Position Size: {config.Risk.MaxPositionSize:P}
- Max Drawdown: {config.Risk.MaxDrawdown:P}
- Stop Loss: {config.Risk.StopLossPercent:P}
- Max Daily Loss: {config.Risk.MaxDailyLoss:P}
- Max Open Positions: {config.Risk.MaxOpenPositions}

TradingView Integration:
- Webhook Port: {config.TradingView.WebhookPort}
- Auto Trade: {config.TradingView.AutoTrade}
- Webhook URL: {config.TradingView.WebhookUrl}
";
    }
}
