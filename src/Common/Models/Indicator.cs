using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace TradingSystem.Common.Models;

public class Indicator
{
    private string _parametersJson = "{}";
    private string _valuesJson = "{}";
    private string _settingsJson = "{}";
    private string _dependenciesJson = "[]";

    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public IndicatorType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }

    public string ParametersJson
    {
        get => _parametersJson;
        set => _parametersJson = value;
    }

    public string ValuesJson
    {
        get => _valuesJson;
        set => _valuesJson = value;
    }

    public string SettingsJson
    {
        get => _settingsJson;
        set => _settingsJson = value;
    }

    public string DependenciesJson
    {
        get => _dependenciesJson;
        set => _dependenciesJson = value;
    }

    [NotMapped]
    public Dictionary<string, decimal> Parameters
    {
        get => JsonSerializer.Deserialize<Dictionary<string, decimal>>(_parametersJson) ?? new();
        set => _parametersJson = JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public Dictionary<string, decimal> Values
    {
        get => JsonSerializer.Deserialize<Dictionary<string, decimal>>(_valuesJson) ?? new();
        set => _valuesJson = JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public Dictionary<string, string> Settings
    {
        get => JsonSerializer.Deserialize<Dictionary<string, string>>(_settingsJson) ?? new();
        set => _settingsJson = JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public List<string> Dependencies
    {
        get => JsonSerializer.Deserialize<List<string>>(_dependenciesJson) ?? new();
        set => _dependenciesJson = JsonSerializer.Serialize(value);
    }
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
