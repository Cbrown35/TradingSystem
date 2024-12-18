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
    public static IServiceProvider CreateTradingSystem(TradingEnvironment environment)
    {
        var services = new ServiceCollection();
        var config = EnvironmentConfigFactory.CreateConfig(environment);

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

        // Register validation service
        services.AddSingleton<IRiskValidationService, RiskValidationService>();

        // Register exchange adapter based on environment
        services.AddSingleton<IExchangeAdapter>(provider =>
        {
            if (config.Environment == TradingEnvironment.Test)
            {
                return new SimulatedExchangeAdapter(
                    provider.GetRequiredService<ILogger<SimulatedExchangeAdapter>>());
            }
            else
            {
                var tradovateConfig = new RealTrading.Models.TradovateConfig
                {
                    ApiKey = config.Tradovate.AppId,
                    ApiSecret = config.Tradovate.Password,
                    AccountId = config.Tradovate.CustomerId,
                    RestApiUrl = config.Tradovate.BaseUrl,
                    WebSocketUrl = config.Tradovate.BaseUrl.Replace("http", "ws"),
                    UseDemoAccount = config.Tradovate.IsPaperTrading
                };

                var tradovateAdapter = new TradovateAdapter(
                    tradovateConfig,
                    provider.GetRequiredService<ILogger<TradovateAdapter>>());

                if (config.Environment == TradingEnvironment.Development)
                {
                    // Wrap with TradingView adapter in dev for testing webhooks
                    var tradingViewConfig = new RealTrading.Models.TradingViewConfig
                    {
                        WebhookPort = config.TradingView.WebhookPort,
                        WebhookEndpoint = config.TradingView.WebhookUrl,
                        AllowedIps = Array.Empty<string>() // Allow all IPs in dev
                    };

                    return new TradingViewAdapter(
                        tradingViewConfig,
                        provider.GetRequiredService<ILogger<TradingViewAdapter>>());
                }
                return tradovateAdapter;
            }
        });

        // Register market data service
        services.AddSingleton<IMarketDataService, MarketDataService>();

        // Register risk manager with environment-specific settings
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
                    ["TakeProfitPercent"] = config.Risk.MaxPositionSize * 2 // Example: 2:1 reward/risk ratio
                }
            };

            var riskManagerConfig = new RiskManagerConfig
            {
                MaxPositionSize = config.Risk.MaxPositionSize,
                DefaultStopLossPercent = config.Risk.StopLossPercent,
                MaxDrawdown = config.Risk.MaxDrawdown,
                MaxRiskPerTrade = config.Risk.MaxDailyLoss,
                MaxPortfolioRisk = config.Risk.MaxPositionSize,
                MinPositionSize = 0.001m, // Default value
                DefaultTakeProfitPercent = config.Risk.MaxPositionSize * 2,
                MinRiskRewardRatio = 1.5m, // Default value
                MaxLeverage = 3m, // Default value
                InitialAccountEquity = 10000m // Default value
            };

            return new RiskManager(
                provider.GetRequiredService<ITradeRepository>(),
                provider.GetRequiredService<IRiskValidationService>(),
                Microsoft.Extensions.Options.Options.Create(riskManagerConfig),
                provider.GetRequiredService<ILogger<RiskManager>>());
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
