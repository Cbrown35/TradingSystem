namespace TradingSystem.Core.Configuration;

public class TradingEnvironmentConfig
{
    public TradingEnvironment Environment { get; set; } = TradingEnvironment.Test;
    public ExchangeConfig? Exchange { get; set; }
    public RiskConfig Risk { get; set; } = new();
    public DatabaseConfig Database { get; set; } = new();
    public TradovateConfig Tradovate { get; set; } = new();
    public TradingViewConfig TradingView { get; set; } = new();
}

public class ExchangeConfig
{
    public string? Type { get; set; }
    public string[]? Symbols { get; set; }
}

public class RiskConfig
{
    public decimal MaxPositionSize { get; set; } = 0.01m;
    public decimal MaxDrawdown { get; set; } = 0.02m;
    public decimal StopLossPercent { get; set; } = 0.02m;
    public decimal TakeProfitPercent { get; set; } = 0.04m;
    public decimal MaxDailyLoss { get; set; } = 0.05m;
    public int MaxOpenPositions { get; set; } = 3;
}

public class DatabaseConfig
{
    public string ConnectionString { get; set; } = "Host=localhost;Database=trading;Username=postgres;Password=postgres";
}

public class TradovateConfig
{
    public string BaseUrl { get; set; } = "https://demo.tradovate.com/v1";
    public string AppId { get; set; } = "";
    public string Password { get; set; } = "";
    public string CustomerId { get; set; } = "";
    public bool IsPaperTrading { get; set; } = true;
}

public class TradingViewConfig
{
    public int WebhookPort { get; set; } = 5000;
    public string WebhookUrl { get; set; } = "/webhook";
    public bool AutoTrade { get; set; } = false;
}
