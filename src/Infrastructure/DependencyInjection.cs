using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradingSystem.Common.Interfaces;
using TradingSystem.Infrastructure.Data;
using TradingSystem.Infrastructure.Repositories;

namespace TradingSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        DatabaseConfig databaseConfig)
    {
        // Register DbContext
        services.AddTradingDatabase(databaseConfig);

        // Register repositories
        services.AddRepositories();

        // Register database configuration
        services.AddSingleton(databaseConfig);

        // Register health checks
        services.AddInfrastructureHealthChecks();

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Register all repositories
        services.AddScoped<ITradeRepository, TradeRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IMarketDataRepository, MarketDataRepository>();

        return services;
    }

    public static IServiceCollection AddInfrastructureHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<TradingContext>("Database");

        return services;
    }

    public static async Task InitializeInfrastructureAsync(
        IServiceProvider serviceProvider,
        DatabaseConfig databaseConfig)
    {
        // Initialize database
        await DatabaseFactory.InitializeDatabaseAsync(serviceProvider, databaseConfig);
    }

    public static async Task<bool> ValidateInfrastructureAsync(IServiceProvider serviceProvider)
    {
        // Test database connection
        var canConnect = await DatabaseFactory.CanConnectAsync(serviceProvider);
        if (!canConnect)
        {
            return false;
        }

        // Add any other infrastructure validation here
        // For example, checking if required tables exist, testing repository operations, etc.

        return true;
    }

    public static async Task BackupInfrastructureAsync(
        IServiceProvider serviceProvider,
        string backupPath)
    {
        // Backup database
        await DatabaseFactory.BackupDatabaseAsync(serviceProvider, backupPath);
    }

    public static async Task RestoreInfrastructureAsync(
        IServiceProvider serviceProvider,
        string backupPath)
    {
        // Restore database
        await DatabaseFactory.RestoreDatabaseAsync(serviceProvider, backupPath);
    }

    public static async Task ResetInfrastructureAsync(IServiceProvider serviceProvider)
    {
        // Delete and recreate database
        await DatabaseFactory.EnsureDatabaseDeletedAsync(serviceProvider);
        await DatabaseFactory.InitializeDatabaseAsync(
            serviceProvider,
            DatabaseExtensions.GetDefaultConfig());
    }

    public static async Task MigrateInfrastructureAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TradingContext>();
        await context.Database.MigrateAsync();
    }

    public static async Task SeedInfrastructureAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TradingContext>();

        // Add any seeding logic here
        // For example:
        // await SeedDefaultDataAsync(context);
    }

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Register any additional infrastructure services here
        // For example:
        // services.AddScoped<ICacheService, CacheService>();
        // services.AddScoped<IFileStorage, FileStorage>();
        // services.AddScoped<IMessageBus, MessageBus>();

        return services;
    }

    public static IServiceCollection AddInfrastructureOptions(
        this IServiceCollection services,
        Action<DatabaseConfig> configureOptions)
    {
        services.Configure(configureOptions);
        return services;
    }

    public static IServiceCollection AddInfrastructureLogging(this IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
            // Add any other logging providers here
        });

        return services;
    }

    public static IServiceCollection AddInfrastructureMonitoring(this IServiceCollection services)
    {
        // Add monitoring services
        // For example:
        // services.AddMetrics();
        // services.AddTracing();
        // services.AddDiagnostics();

        return services;
    }
}
