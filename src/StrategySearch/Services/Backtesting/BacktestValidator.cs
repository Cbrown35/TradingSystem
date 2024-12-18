using TradingSystem.Common.Models;

namespace TradingSystem.StrategySearch.Services.Backtesting;

internal class BacktestValidator
{
    private const int MinimumTrades = 30;
    private const decimal MinimumWinRate = 0.4M;
    private const decimal MinimumSharpeRatio = 1.0M;
    private const decimal MaximumDrawdown = 0.2M;

    public (bool isValid, Dictionary<string, decimal> metrics, Dictionary<string, string> messages) ValidateBacktestResult(BacktestResult result)
    {
        var validationMessages = new Dictionary<string, string>();
        var validationMetrics = new Dictionary<string, decimal>();
        var isValid = true;

        // Check minimum number of trades
        if (result.TotalTrades < MinimumTrades)
        {
            validationMessages["TotalTrades"] = $"Insufficient trades: {result.TotalTrades} (minimum {MinimumTrades})";
            validationMetrics["TotalTradesValid"] = 0;
            isValid = false;
        }
        else
        {
            validationMetrics["TotalTradesValid"] = 1;
        }

        // Check win rate
        if (result.WinRate < MinimumWinRate)
        {
            validationMessages["WinRate"] = $"Low win rate: {result.WinRate:P2} (minimum {MinimumWinRate:P2})";
            validationMetrics["WinRateValid"] = 0;
            isValid = false;
        }
        else
        {
            validationMetrics["WinRateValid"] = 1;
        }

        // Check Sharpe ratio
        if (result.SharpeRatio < MinimumSharpeRatio)
        {
            validationMessages["SharpeRatio"] = $"Low Sharpe ratio: {result.SharpeRatio:F2} (minimum {MinimumSharpeRatio:F2})";
            validationMetrics["SharpeRatioValid"] = 0;
            isValid = false;
        }
        else
        {
            validationMetrics["SharpeRatioValid"] = 1;
        }

        // Check maximum drawdown
        if (result.MaxDrawdown > MaximumDrawdown)
        {
            validationMessages["MaxDrawdown"] = $"High drawdown: {result.MaxDrawdown:P2} (maximum {MaximumDrawdown:P2})";
            validationMetrics["MaxDrawdownValid"] = 0;
            isValid = false;
        }
        else
        {
            validationMetrics["MaxDrawdownValid"] = 1;
        }

        validationMetrics["ValidationStatus"] = isValid ? 1 : 0;

        return (isValid, validationMetrics, validationMessages);
    }
}
