using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

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

    public string ParametersJson { get; set; } = "{}";
    public string MetricsJson { get; set; } = "{}";

    [NotMapped]
    public Dictionary<string, decimal> Parameters
    {
        get => JsonSerializer.Deserialize<Dictionary<string, decimal>>(ParametersJson) ?? new();
        set => ParametersJson = JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public Dictionary<string, decimal> Metrics
    {
        get => JsonSerializer.Deserialize<Dictionary<string, decimal>>(MetricsJson) ?? new();
        set => MetricsJson = JsonSerializer.Serialize(value);
    }

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
                    Id = Guid.NewGuid().ToString(),
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
    [Key]
    public string Id { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public SignalConditionType Type { get; set; }
    public string SignalId { get; set; } = string.Empty;
    
    public string ParametersJson { get; set; } = "{}";

    [NotMapped]
    public Dictionary<string, decimal> Parameters
    {
        get => JsonSerializer.Deserialize<Dictionary<string, decimal>>(ParametersJson) ?? new();
        set => ParametersJson = JsonSerializer.Serialize(value);
    }
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
