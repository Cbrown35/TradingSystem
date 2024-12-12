namespace TradingSystem.Core.Configuration;

public enum TradingEnvironment
{
    Test,
    Development,
    Production
}

public class TradingEnvironmentConfig
{
    public TradingEnvironment Environment { get; set; }
    public DatabaseConfig Database { get; set; } = new();
    public TradovateConfig Tradovate { get; set; } = new();
    public TradingViewConfig TradingView { get; set; } = new();
    public RiskConfig Risk { get; set; } = new();
}

public class DatabaseConfig
{
    public string ConnectionString { get; set; } = string.Empty;
}

public class TradovateConfig
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string AppId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public bool IsPaperTrading { get; set; }
}

public class TradingViewConfig
{
    public int WebhookPort { get; set; } = 5000;
    public bool AutoTrade { get; set; }
    public decimal DefaultQuantity { get; set; } = 1;
    public string WebhookUrl { get; set; } = string.Empty;
}

public class RiskConfig
{
    public decimal MaxPositionSize { get; set; } = 0.02m;
    public decimal MaxDrawdown { get; set; } = 0.10m;
    public decimal StopLossPercent { get; set; } = 0.02m;
    public decimal MaxDailyLoss { get; set; } = 0.05m;
    public int MaxOpenPositions { get; set; } = 5;
}

public static class EnvironmentConfigFactory
{
    public static TradingEnvironmentConfig CreateConfig(TradingEnvironment env)
    {
        return env switch
        {
            TradingEnvironment.Test => new TradingEnvironmentConfig
            {
                Environment = TradingEnvironment.Test,
                Database = new DatabaseConfig
                {
                    ConnectionString = "Server=localhost;Database=TradingSystem_Test;Trusted_Connection=True;"
                },
                Tradovate = new TradovateConfig
                {
                    BaseUrl = "https://demo.tradovate.com/v1",
                    IsPaperTrading = true
                },
                TradingView = new TradingViewConfig
                {
                    WebhookPort = 5001,
                    AutoTrade = false,
                    DefaultQuantity = 1,
                    WebhookUrl = "http://localhost:5001/webhook"
                },
                Risk = new RiskConfig
                {
                    MaxPositionSize = 0.01m,
                    MaxDrawdown = 0.05m,
                    StopLossPercent = 0.01m,
                    MaxDailyLoss = 0.02m,
                    MaxOpenPositions = 2
                }
            },

            TradingEnvironment.Development => new TradingEnvironmentConfig
            {
                Environment = TradingEnvironment.Development,
                Database = new DatabaseConfig
                {
                    ConnectionString = "Server=localhost;Database=TradingSystem_Dev;Trusted_Connection=True;"
                },
                Tradovate = new TradovateConfig
                {
                    BaseUrl = "https://demo.tradovate.com/v1",
                    IsPaperTrading = true
                },
                TradingView = new TradingViewConfig
                {
                    WebhookPort = 5000,
                    AutoTrade = true,
                    DefaultQuantity = 1,
                    WebhookUrl = "http://localhost:5000/webhook"
                },
                Risk = new RiskConfig
                {
                    MaxPositionSize = 0.02m,
                    MaxDrawdown = 0.08m,
                    StopLossPercent = 0.02m,
                    MaxDailyLoss = 0.04m,
                    MaxOpenPositions = 3
                }
            },

            TradingEnvironment.Production => new TradingEnvironmentConfig
            {
                Environment = TradingEnvironment.Production,
                Database = new DatabaseConfig
                {
                    ConnectionString = "Server=prod-server;Database=TradingSystem;Trusted_Connection=True;"
                },
                Tradovate = new TradovateConfig
                {
                    BaseUrl = "https://live.tradovate.com/v1",
                    IsPaperTrading = false
                },
                TradingView = new TradingViewConfig
                {
                    WebhookPort = 443,
                    AutoTrade = true,
                    DefaultQuantity = 1,
                    WebhookUrl = "https://your-domain.com/webhook"
                },
                Risk = new RiskConfig
                {
                    MaxPositionSize = 0.02m,
                    MaxDrawdown = 0.10m,
                    StopLossPercent = 0.02m,
                    MaxDailyLoss = 0.05m,
                    MaxOpenPositions = 5
                }
            },

            _ => throw new ArgumentException($"Unsupported environment: {env}")
        };
    }
}
