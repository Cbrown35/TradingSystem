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
            var conditionStr = condition.Type switch
            {
                ConditionType.CrossOver => $"ta.crossover({condition.LeftOperand}, {condition.RightOperand})",
                ConditionType.CrossUnder => $"ta.crossunder({condition.LeftOperand}, {condition.RightOperand})",
                ConditionType.GreaterThan => $"{condition.LeftOperand} > {condition.RightOperand}",
                ConditionType.LessThan => $"{condition.LeftOperand} < {condition.RightOperand}",
                ConditionType.Equals => $"{condition.LeftOperand} == {condition.RightOperand}",
                _ => throw new ArgumentException($"Unsupported condition type: {condition.Type}")
            };
            conditions.Add(conditionStr);
        }

        var conditionName = signal.Name.ToLower() + "Condition";
        return $"{conditionName} = {string.Join(" and ", conditions)}";
    }
}
