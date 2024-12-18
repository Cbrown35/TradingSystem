using TradingSystem.Common.Models;

namespace TradingSystem.Core.Monitoring.Extensions;

public static class RiskMetricsExtensions
{
    public static decimal GetCurrentDrawdown(this RiskMetrics metrics)
    {
        // Calculate current drawdown from equity curve
        if (metrics.EquityHistory == null || !metrics.EquityHistory.Any())
            return 0;

        var peak = metrics.EquityHistory.Max();
        var current = metrics.EquityHistory.Last();
        return peak > 0 ? (peak - current) / peak : 0;
    }

    public static decimal GetMaxAllowedDrawdown(this RiskMetrics metrics)
    {
        // Default to 20% if not specified in risk parameters
        return metrics.RiskParameters.GetValueOrDefault("MaxAllowedDrawdown", 0.20m);
    }

    public static decimal GetTotalPositionExposure(this RiskMetrics metrics)
    {
        return metrics.OpenPositions?.Sum(p => Math.Abs(p.Value)) ?? 0;
    }

    public static decimal GetMaxPositionExposure(this RiskMetrics metrics)
    {
        // Default to 100% of account equity if not specified
        return metrics.RiskParameters.GetValueOrDefault("MaxPositionExposure", metrics.AccountEquity);
    }

    public static decimal GetMarginUsagePercent(this RiskMetrics metrics)
    {
        if (metrics.AccountEquity <= 0)
            return 0;

        return (metrics.MarginUsed / metrics.AccountEquity) * 100;
    }

    public static decimal GetAverageRiskPerTrade(this RiskMetrics metrics)
    {
        if (metrics.TotalTrades <= 0)
            return 0;

        return metrics.TotalRisk / metrics.TotalTrades;
    }

    public static decimal GetMaxRiskPerTrade(this RiskMetrics metrics)
    {
        // Default to 2% of account equity if not specified
        return metrics.RiskParameters.GetValueOrDefault("MaxRiskPerTrade", metrics.AccountEquity * 0.02m);
    }
}
