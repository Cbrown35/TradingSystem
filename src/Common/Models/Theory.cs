namespace TradingSystem.Common.Models;

public class Theory
{
    public string Name { get; set; } = string.Empty;
    public List<string> Symbols { get; set; } = new();
    public List<Indicator> Indicators { get; set; } = new();
    public Signal OpenSignal { get; set; } = new();
    public Signal CloseSignal { get; set; } = new();
    public Dictionary<string, decimal> Parameters { get; set; } = new();
}

public class Indicator
{
    public string Name { get; set; } = string.Empty;
    public IndicatorType Type { get; set; }
    public Dictionary<string, decimal> Parameters { get; set; } = new();
}

public enum IndicatorType
{
    SMA,
    EMA,
    RSI,
    MACD,
    Bollinger,
    ATR
}

public class Signal
{
    public string Name { get; set; } = string.Empty;
    public List<Condition> Conditions { get; set; } = new();
}

public class Condition
{
    public string LeftOperand { get; set; } = string.Empty;
    public string RightOperand { get; set; } = string.Empty;
    public ConditionType Type { get; set; }
}

public enum ConditionType
{
    CrossOver,
    CrossUnder,
    GreaterThan,
    LessThan,
    Equals
}
