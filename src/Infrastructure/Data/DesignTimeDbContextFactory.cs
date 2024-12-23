using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TradingSystem.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TradingContext>
{
    public TradingContext CreateDbContext(string[] args)
    {
        // Load configuration from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<TradingContext>();
        
        // Get connection string from configuration
        var connectionString = configuration.GetConnectionString("DefaultConnection") ??
            // Fallback to a default local connection if not configured
            "Host=localhost;Database=tradingsystem;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString, options =>
        {
            options.EnableRetryOnFailure();
            options.CommandTimeout(60);
        });

        return new TradingContext(optionsBuilder.Options);
    }
}
