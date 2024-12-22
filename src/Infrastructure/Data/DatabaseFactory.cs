using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

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

            // Enable TimescaleDB extension
            await context.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS timescaledb CASCADE;");

            // Create hypertables and continuous aggregates
            await context.CreateHypertablesAsync();

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
            {
                // Verify TimescaleDB extension
                var hasTimescaleDB = await context.Database
                    .SqlQuery<bool>($"SELECT COUNT(*) > 0 FROM pg_extension WHERE extname = 'timescaledb';")
                    .SingleOrDefaultAsync();

                if (!hasTimescaleDB)
                {
                    logger.LogWarning("TimescaleDB extension is not installed.");
                    return false;
                }

                logger.LogInformation("Successfully connected to the database with TimescaleDB.");
            }
            else
            {
                logger.LogWarning("Could not connect to the database.");
            }

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
            
            // Use Process to execute pg_dump with TimescaleDB support
            var startInfo = new ProcessStartInfo
            {
                FileName = "pg_dump",
                Arguments = $"-Fc --no-owner --no-acl \"{context.Database.GetConnectionString()}\" -f \"{backupPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new Exception("Failed to start pg_dump process");
            }

            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"pg_dump failed with exit code {process.ExitCode}: {error}");
            }
            
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

            // Enable TimescaleDB extension before restore
            await context.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS timescaledb CASCADE;");

            // Use Process to execute pg_restore
            var startInfo = new ProcessStartInfo
            {
                FileName = "pg_restore",
                Arguments = $"-d \"{context.Database.GetConnectionString()}\" --no-owner --no-acl \"{backupPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new Exception("Failed to start pg_restore process");
            }

            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"pg_restore failed with exit code {process.ExitCode}: {error}");
            }
            
            // Recreate hypertables after restore
            await context.CreateHypertablesAsync();
            
            logger.LogInformation("Database restore completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while restoring the database.");
            throw;
        }
    }
}
