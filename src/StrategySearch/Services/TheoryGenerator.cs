using TradingSystem.Common.Interfaces;
using TradingSystem.Common.Models;

namespace TradingSystem.StrategySearch.Services;

public class TheoryGenerator : ITheoryGenerator
{
    private readonly Random _random = new();

    public async Task<Theory> GenerateTheory(string[] symbols)
    {
        var theory = new Theory
        {
            Name = $"Theory_{DateTime.UtcNow:yyyyMMddHHmmss}",
            Symbols = symbols.ToList(),
            Indicators = GenerateIndicators(),
            Parameters = GenerateParameters()
        };

        theory.OpenSignal = GenerateSignal("Entry", theory.Indicators);
        theory.CloseSignal = GenerateSignal("Exit", theory.Indicators);

        return theory;
    }

    public async Task<List<Theory>> GenerateTheories(string[] symbols, int count)
    {
        var theories = new List<Theory>();
        for (int i = 0; i < count; i++)
        {
            var theory = await GenerateTheory(symbols);
            theories.Add(theory);
        }
        return theories;
    }

    public async Task<Theory> MutateTheory(Theory theory)
    {
        // Randomly mutate indicators, parameters, or signals
        var rnd = new Random();
        if (rnd.Next(2) == 0)
        {
            theory.Indicators = GenerateIndicators();
        }
        else if (rnd.Next(2) == 0) 
        {
            theory.Parameters = GenerateParameters();
        }
        else
        {
            theory.OpenSignal = GenerateSignal("Entry", theory.Indicators);
            theory.CloseSignal = GenerateSignal("Exit", theory.Indicators);
        }
        return theory;
    }

    public async Task<Theory> CrossoverTheories(Theory theory1, Theory theory2)
    {
        var theory = new Theory
        {
            Name = $"Cross_{DateTime.UtcNow:yyyyMMddHHmmss}",
            Symbols = theory1.Symbols,
            Indicators = new List<Indicator>(),
            Parameters = new Dictionary<string, decimal>()
        };

        // Randomly select indicators and parameters from parents
        var rnd = new Random();
        foreach (var indicator in theory1.Indicators.Concat(theory2.Indicators))
        {
            if (rnd.Next(2) == 0)
                theory.Indicators.Add(indicator);
        }
        foreach (var kvp in theory1.Parameters.Concat(theory2.Parameters))
        {
            if (rnd.Next(2) == 0)
                theory.Parameters[kvp.Key] = kvp.Value;
        }

        theory.OpenSignal = rnd.Next(2) == 0 ? theory1.OpenSignal : theory2.OpenSignal;
        theory.CloseSignal = rnd.Next(2) == 0 ? theory1.CloseSignal : theory2.CloseSignal;

        return theory;
    }

    private List<Indicator> GenerateIndicators()
    {
        var indicators = new List<Indicator>();
        var numIndicators = _random.Next(2, 5);

        for (int i = 0; i < numIndicators; i++)
        {
            var type = (IndicatorType)_random.Next(Enum.GetValues(typeof(IndicatorType)).Length);
            var indicator = new Indicator
            {
                Name = $"{type}_{i}",
                Type = type,
                Parameters = GenerateIndicatorParameters(type)
            };
            indicators.Add(indicator);
        }

        return indicators;
    }

    private Dictionary<string, decimal> GenerateIndicatorParameters(IndicatorType type)
    {
        var parameters = new Dictionary<string, decimal>();

        switch (type)
        {
            case IndicatorType.SMA:
            case IndicatorType.EMA:
            case IndicatorType.RSI:
                parameters["Period"] = _random.Next(5, 50);
                break;

            case IndicatorType.MACD:
                parameters["FastPeriod"] = _random.Next(8, 15);
                parameters["SlowPeriod"] = _random.Next(20, 30);
                parameters["SignalPeriod"] = _random.Next(5, 10);
                break;

            case IndicatorType.Bollinger:
                parameters["Period"] = _random.Next(10, 30);
                parameters["Multiplier"] = _random.Next(15, 25) / 10m;
                break;

            case IndicatorType.ATR:
                parameters["Period"] = _random.Next(10, 20);
                break;
        }

        return parameters;
    }

    private Signal GenerateSignal(string name, List<Indicator> indicators)
    {
        var signal = new Signal 
        { 
            Name = name,
            Id = Guid.NewGuid().ToString(),
            Type = name == "Entry" ? SignalType.Entry : SignalType.Exit,
            Strength = SignalStrength.Medium,
            Timestamp = DateTime.UtcNow
        };

        var numConditions = _random.Next(1, 3);

        for (int i = 0; i < numConditions; i++)
        {
            var leftOperand = GetRandomIndicatorReference(indicators);
            var rightOperand = GetRandomIndicatorReference(indicators);
            var conditionType = GetRandomSignalConditionType();

            var signalCondition = new SignalCondition
            {
                Type = conditionType,
                Expression = $"{leftOperand} {GetOperatorString(conditionType)} {rightOperand}",
                Parameters = new Dictionary<string, decimal>()
            };

            signal.Conditions.Add(signalCondition);
        }

        // Build the complete expression from all conditions
        signal.Expression = string.Join(" AND ", signal.Conditions.Select(c => c.Expression));

        return signal;
    }

    private SignalConditionType GetRandomSignalConditionType()
    {
        var types = new[]
        {
            SignalConditionType.PriceAbove,
            SignalConditionType.PriceBelow,
            SignalConditionType.CrossOver,
            SignalConditionType.CrossUnder
        };
        return types[_random.Next(types.Length)];
    }

    private string GetOperatorString(SignalConditionType type)
    {
        return type switch
        {
            SignalConditionType.PriceAbove => ">",
            SignalConditionType.PriceBelow => "<",
            SignalConditionType.CrossOver => "crosses above",
            SignalConditionType.CrossUnder => "crosses below",
            _ => "=="
        };
    }

    private string GetRandomIndicatorReference(List<Indicator> indicators)
    {
        if (_random.Next(2) == 0 && indicators.Any())
        {
            var indicator = indicators[_random.Next(indicators.Count)];
            return indicator.Name;
        }
        
        // Return a price reference
        var priceTypes = new[] { "close", "open", "high", "low" };
        return priceTypes[_random.Next(priceTypes.Length)];
    }

    private Dictionary<string, decimal> GenerateParameters()
    {
        return new Dictionary<string, decimal>
        {
            { "RiskPerTrade", 0.02m },
            { "MaxDrawdown", 0.10m },
            { "MinProfitFactor", 1.5m }
        };
    }
}
