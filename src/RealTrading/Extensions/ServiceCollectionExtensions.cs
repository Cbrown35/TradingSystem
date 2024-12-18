using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradingSystem.Common.Interfaces;
using TradingSystem.RealTrading.Configuration;
using TradingSystem.RealTrading.Services;

namespace TradingSystem.RealTrading.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTradingServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure market data caching
        services.Configure<MarketDataCacheConfig>(configuration.GetSection("MarketDataCache"));
        services.AddMemoryCache(options =>
        {
            var config = configuration.GetSection("MarketDataCache").Get<MarketDataCacheConfig>();
            if (config != null)
            {
                options.SizeLimit = (long?)config.MaxCacheItems;
            }
        });

        // Configure risk management
        services.Configure<RiskManagerConfig>(configuration.GetSection("RiskManagement"));

        // Register services
        services.AddSingleton<IMarketDataCacheService, MarketDataCacheService>();
        services.AddScoped<IMarketDataService, MarketDataService>();
        services.AddScoped<IRiskValidationService, RiskValidationService>();
        services.AddScoped<IRiskManager, RiskManager>();

        return services;
    }
}
