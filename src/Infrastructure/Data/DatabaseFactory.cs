using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TradingSystem.Infrastructure.Data;

public static class DatabaseFactory
{
    public static IServiceCollection AddTradingDatabase(
        this IServiceCollection services,
        DatabaseConfig config)
    {
        services.AddDbContext<TradingContext>((serviceProvider, options) =>
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

            options
                .UseNpgsql(config.ConnectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(config.MigrationsAssembly);
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: config.MaxRetryCount,
                        maxRetryDelay: config.MaxRetryDelay,
                        errorCodesToAdd: null);
                    npgsqlOptions.CommandTimeout(config.CommandTimeout);
                })
                .UseLoggerFactory(loggerFactory)
                .EnableSensitiveDataLogging(config.EnableSensitiveDataLogging)
                .EnableDetailedErrors(config.EnableDetailedErrors);
        });

        return services;
    }

    public static async Task InitializeDatabaseAsync(
        IServiceProvider serviceProvider,
        DatabaseConfig config)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TradingContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TradingContext>>();

        try
        {
            if (config.EnableAutoMigration)
            {
                logger.LogInformation("Applying database migrations...");
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied successfully.");
            }

            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Set default schema
            await context.Database.ExecuteSqlRawAsync($"SET search_path TO {config.Schema};");

            // Additional initialization if needed
            await InitializeDefaultDataAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database.");
            throw;
        }
    }

    private static async Task InitializeDefaultDataAsync(
        TradingContext context,
        ILogger<TradingContext> logger)
    {
        try
        {
            // Check if we need to seed any default data
            if (!await context.Theories.AnyAsync())
            {
                logger.LogInformation("Seeding default theories...");
                // Add default theories if needed
            }

            // Add any other default data seeding here

            await context.SaveChangesAsync();
            logger.LogInformation("Default data seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding default data.");
            throw;
        }
    }

    public static async Task EnsureDatabaseDeletedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TradingContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TradingContext>>();

        try
        {
            logger.LogWarning("Deleting database...");
            await context.Database.EnsureDeletedAsync();
            logger.LogInformation("Database deleted successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while deleting the database.");
            throw;
        }
    }

    public static async Task<bool> CanConnectAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TradingContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TradingContext>>();

        try
        {
            logger.LogInformation("Testing database connection...");
            var canConnect = await context.Database.CanConnectAsync();
            
            if (canConnect)
                logger.LogInformation("Successfully connected to the database.");
            else
                logger.LogWarning("Could not connect to the database.");

            return canConnect;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while testing database connection.");
            return false;
        }
    }

    public static async Task BackupDatabaseAsync(
        IServiceProvider serviceProvider,
        string backupPath)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TradingContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TradingContext>>();

        try
        {
            logger.LogInformation("Starting database backup...");
            
            // Execute pg_dump command through npgsql
            var connectionString = context.Database.GetConnectionString();
            var backupCommand = $"pg_dump -Fc \"{connectionString}\" > \"{backupPath}\"";
            
            await context.Database.ExecuteSqlRawAsync(backupCommand);
            
            logger.LogInformation("Database backup completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while backing up the database.");
            throw;
        }
    }

    public static async Task RestoreDatabaseAsync(
        IServiceProvider serviceProvider,
        string backupPath)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TradingContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TradingContext>>();

        try
        {
            logger.LogInformation("Starting database restore...");
            
            // First ensure database exists
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();

            // Execute pg_restore command through npgsql
            var connectionString = context.Database.GetConnectionString();
            var restoreCommand = $"pg_restore -d \"{connectionString}\" \"{backupPath}\"";
            
            await context.Database.ExecuteSqlRawAsync(restoreCommand);
            
            logger.LogInformation("Database restore completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while restoring the database.");
            throw;
        }
    }
}
