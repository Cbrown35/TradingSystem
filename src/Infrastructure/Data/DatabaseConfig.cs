namespace TradingSystem.Infrastructure.Data;

public class DatabaseConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public bool EnableSensitiveDataLogging { get; set; }
    public bool EnableDetailedErrors { get; set; }
    public int CommandTimeout { get; set; } = 30;
    public int MaxRetryCount { get; set; } = 3;
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);
    public bool EnableAutoMigration { get; set; }
    public string MigrationsAssembly { get; set; } = "TradingSystem.Infrastructure";
    public string Schema { get; set; } = "public";
}

public static class DatabaseExtensions
{
    public static string GetDefaultConnectionString()
    {
        return "Host=localhost;Database=tradingsystem;Username=postgres;Password=postgres";
    }

    public static DatabaseConfig GetDefaultConfig()
    {
        return new DatabaseConfig
        {
            ConnectionString = GetDefaultConnectionString(),
            EnableSensitiveDataLogging = false,
            EnableDetailedErrors = false,
            CommandTimeout = 30,
            MaxRetryCount = 3,
            MaxRetryDelay = TimeSpan.FromSeconds(30),
            EnableAutoMigration = true,
            Schema = "public"
        };
    }
}
