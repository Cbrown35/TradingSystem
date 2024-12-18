namespace TradingSystem.Common.Models;

public class Indicator
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public IndicatorType Type { get; set; }
    public Dictionary<string, decimal> Parameters { get; set; } = new();
    public Dictionary<string, decimal> Values { get; set; } = new();
    public Dictionary<string, string> Settings { get; set; } = new();
    public List<string> Dependencies { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}

public enum IndicatorType
{
    Trend,
    Momentum,
    Volatility,
    Volume,
    Custom,
    SMA,
    EMA,
    RSI,
    MACD,
    Bollinger,
    ATR
}

public class Condition
{
    public string Id { get; set; } = string.Empty;
    public ConditionType Type { get; set; }
    public string IndicatorId { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string TimeFrame { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public string LeftOperand { get; set; } = string.Empty;
    public string RightOperand { get; set; } = string.Empty;
}

public enum ConditionType
{
    CrossOver,
    CrossUnder,
    GreaterThan,
    LessThan,
    Equals,
    Between,
    Outside,
    Custom
}
