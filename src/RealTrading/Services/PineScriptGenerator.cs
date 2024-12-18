using System.Text;
using TradingSystem.Common.Models;

namespace TradingSystem.RealTrading.Services;

public class PineScriptGenerator
{
    public string GenerateScript(Theory theory)
    {
        var sb = new StringBuilder();

        // Add script header
        sb.AppendLine("//@version=5");
        sb.AppendLine($"strategy(\"{theory.Name}\", overlay=true)");
        sb.AppendLine();

        // Add indicator calculations
        foreach (var indicator in theory.Indicators)
        {
            sb.AppendLine(GenerateIndicatorCode(indicator));
        }
        sb.AppendLine();

        // Add entry conditions
        sb.AppendLine("// Entry conditions");
        sb.AppendLine(GenerateSignalConditions(theory.OpenSignal));
        sb.AppendLine("if (entryCondition)");
        sb.AppendLine("    strategy.entry(\"Long\", strategy.long)");
        sb.AppendLine();

        // Add exit conditions
        sb.AppendLine("// Exit conditions");
        sb.AppendLine(GenerateSignalConditions(theory.CloseSignal));
        sb.AppendLine("if (exitCondition)");
        sb.AppendLine("    strategy.close(\"Long\")");

        return sb.ToString();
    }

    private string GenerateIndicatorCode(Indicator indicator)
    {
        return indicator.Type switch
        {
            IndicatorType.SMA => GenerateSMACode(indicator),
            IndicatorType.EMA => GenerateEMACode(indicator),
            IndicatorType.RSI => GenerateRSICode(indicator),
            IndicatorType.MACD => GenerateMACDCode(indicator),
            IndicatorType.Bollinger => GenerateBollingerCode(indicator),
            IndicatorType.ATR => GenerateATRCode(indicator),
            _ => throw new ArgumentException($"Unsupported indicator type: {indicator.Type}")
        };
    }

    private string GenerateSMACode(Indicator indicator)
    {
        var period = indicator.Parameters.GetValueOrDefault("Period", 20);
        return $"{indicator.Name} = ta.sma(close, {period})";
    }

    private string GenerateEMACode(Indicator indicator)
    {
        var period = indicator.Parameters.GetValueOrDefault("Period", 20);
        return $"{indicator.Name} = ta.ema(close, {period})";
    }

    private string GenerateRSICode(Indicator indicator)
    {
        var period = indicator.Parameters.GetValueOrDefault("Period", 14);
        return $"{indicator.Name} = ta.rsi(close, {period})";
    }

    private string GenerateMACDCode(Indicator indicator)
    {
        var fastPeriod = indicator.Parameters.GetValueOrDefault("FastPeriod", 12);
        var slowPeriod = indicator.Parameters.GetValueOrDefault("SlowPeriod", 26);
        var signalPeriod = indicator.Parameters.GetValueOrDefault("SignalPeriod", 9);
        return $"[{indicator.Name}_macd, {indicator.Name}_signal, {indicator.Name}_hist] = ta.macd(close, {fastPeriod}, {slowPeriod}, {signalPeriod})";
    }

    private string GenerateBollingerCode(Indicator indicator)
    {
        var period = indicator.Parameters.GetValueOrDefault("Period", 20);
        var multiplier = indicator.Parameters.GetValueOrDefault("Multiplier", 2);
        return $"[{indicator.Name}_middle, {indicator.Name}_upper, {indicator.Name}_lower] = ta.bb(close, {period}, {multiplier})";
    }

    private string GenerateATRCode(Indicator indicator)
    {
        var period = indicator.Parameters.GetValueOrDefault("Period", 14);
        return $"{indicator.Name} = ta.atr({period})";
    }

    private string GenerateSignalConditions(Signal signal)
    {
        var conditions = new List<string>();

        foreach (var condition in signal.Conditions)
        {
            // Parse the expression to get operands
            var parts = ParseExpression(condition.Expression);
            
            var conditionStr = condition.Type switch
            {
                SignalConditionType.CrossOver => $"ta.crossover({parts.leftOperand}, {parts.rightOperand})",
                SignalConditionType.CrossUnder => $"ta.crossunder({parts.leftOperand}, {parts.rightOperand})",
                SignalConditionType.PriceAbove => $"{parts.leftOperand} > {parts.rightOperand}",
                SignalConditionType.PriceBelow => $"{parts.leftOperand} < {parts.rightOperand}",
                SignalConditionType.Custom => condition.Expression,
                _ => throw new ArgumentException($"Unsupported condition type: {condition.Type}")
            };
            conditions.Add(conditionStr);
        }

        var conditionName = signal.Name.ToLower() + "Condition";
        return $"{conditionName} = {string.Join(" and ", conditions)}";
    }

    private (string leftOperand, string rightOperand) ParseExpression(string expression)
    {
        // Split on common operators
        var parts = expression.Split(new[] { " > ", " < ", " >= ", " <= ", " == ", " crosses above ", " crosses below " }, 
            StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 2)
        {
            // If we can't parse it, return the whole expression as the left operand
            return (expression.Trim(), "0");
        }

        return (parts[0].Trim(), parts[1].Trim());
    }
}
