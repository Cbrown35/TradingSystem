namespace TradingSystem.Common.Models;

public class Signal
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public SignalType Type { get; set; }
    public SignalStrength Strength { get; set; }
    public DateTime Timestamp { get; set; }
    public List<SignalCondition> Conditions { get; set; } = new();
    public Dictionary<string, decimal> Parameters { get; set; } = new();
    public Dictionary<string, decimal> Metrics { get; set; } = new();

    public static Signal FromString(string signal)
    {
        return new Signal
        {
            Id = Guid.NewGuid().ToString(),
            Name = signal,
            Expression = signal,
            Type = SignalType.Custom,
            Strength = SignalStrength.Medium,
            Timestamp = DateTime.UtcNow,
            Conditions = new List<SignalCondition>
            {
                new SignalCondition
                {
                    Expression = signal,
                    Type = SignalConditionType.Custom
                }
            }
        };
    }

    public static implicit operator Signal(string signal) => FromString(signal);
}

public class SignalCondition
{
    public string Expression { get; set; } = string.Empty;
    public SignalConditionType Type { get; set; }
    public Dictionary<string, decimal> Parameters { get; set; } = new();
}

public enum SignalType
{
    Entry,
    Exit,
    StopLoss,
    TakeProfit,
    Custom
}

public enum SignalStrength
{
    VeryWeak,
    Weak,
    Medium,
    Strong,
    VeryStrong
}

public enum SignalConditionType
{
    PriceAbove,
    PriceBelow,
    CrossOver,
    CrossUnder,
    Divergence,
    Convergence,
    Custom
}
