using TradingSystem.Common.Models;

namespace TradingSystem.StrategySearch.Services.Backtesting;

internal class BacktestMetricsCalculator
{
    private const decimal RiskFreeRate = 0.02M; // 2% annual risk-free rate

    public void CalculateMetrics(BacktestResult result, decimal finalEquity)
    {
        result.FinalEquity = finalEquity;
        result.TotalTrades = result.Trades.Count;
        result.WinningTrades = result.Trades.Count(t => (t.RealizedPnL ?? 0) > 0);
        result.LosingTrades = result.Trades.Count(t => (t.RealizedPnL ?? 0) <= 0);
        result.WinRate = result.TotalTrades > 0 ? (decimal)result.WinningTrades / result.TotalTrades : 0;
        result.ProfitFactor = CalculateProfitFactor(result.Trades);
        result.MaxDrawdown = CalculateMaxDrawdown(result.Trades);

        var winningTrades = result.Trades.Where(t => (t.RealizedPnL ?? 0) > 0).ToList();
        var losingTrades = result.Trades.Where(t => (t.RealizedPnL ?? 0) <= 0).ToList();

        result.AverageWin = winningTrades.Any() ? winningTrades.Average(t => t.RealizedPnL ?? 0) : 0;
        result.AverageLoss = losingTrades.Any() ? Math.Abs(losingTrades.Average(t => t.RealizedPnL ?? 0)) : 0;
        result.LargestWin = winningTrades.Any() ? winningTrades.Max(t => t.RealizedPnL ?? 0) : 0;
        result.LargestLoss = losingTrades.Any() ? Math.Abs(losingTrades.Min(t => t.RealizedPnL ?? 0)) : 0;

        result.MaxConsecutiveWins = CalculateMaxConsecutive(result.Trades, true);
        result.MaxConsecutiveLosses = CalculateMaxConsecutive(result.Trades, false);

        result.AverageHoldingPeriod = result.Trades.Any() 
            ? (decimal)result.Trades.Average(t => (t.CloseTime!.Value - t.OpenTime).TotalDays) 
            : 0M;

        result.SharpeRatio = CalculateSharpeRatio(result.Trades);
        result.SortinoRatio = CalculateSortinoRatio(result.Trades);
    }

    private decimal CalculateProfitFactor(List<Trade> trades)
    {
        var grossProfit = trades.Where(t => (t.RealizedPnL ?? 0) > 0).Sum(t => t.RealizedPnL ?? 0);
        var grossLoss = Math.Abs(trades.Where(t => (t.RealizedPnL ?? 0) <= 0).Sum(t => t.RealizedPnL ?? 0));

        return grossLoss > 0 ? grossProfit / grossLoss : 0;
    }

    private decimal CalculateMaxDrawdown(List<Trade> trades)
    {
        decimal peak = 0;
        decimal maxDrawdown = 0;
        decimal currentEquity = 10000m;

        foreach (var trade in trades.OrderBy(t => t.OpenTime))
        {
            currentEquity += trade.RealizedPnL ?? 0;
            
            if (currentEquity > peak)
            {
                peak = currentEquity;
            }

            var drawdown = peak > 0 ? (peak - currentEquity) / peak : 0;
            maxDrawdown = Math.Max(maxDrawdown, drawdown);
        }

        return maxDrawdown;
    }

    private int CalculateMaxConsecutive(List<Trade> trades, bool winning)
    {
        int maxConsecutive = 0;
        int currentConsecutive = 0;

        foreach (var trade in trades.OrderBy(t => t.OpenTime))
        {
            if ((winning && (trade.RealizedPnL ?? 0) > 0) || (!winning && (trade.RealizedPnL ?? 0) <= 0))
            {
                currentConsecutive++;
                maxConsecutive = Math.Max(maxConsecutive, currentConsecutive);
            }
            else
            {
                currentConsecutive = 0;
            }
        }

        return maxConsecutive;
    }

    private decimal CalculateSharpeRatio(List<Trade> trades)
    {
        var returns = trades.Select(t => (t.RealizedPnL ?? 0) / 10000m).ToList();
        var averageReturn = returns.Any() ? returns.Average() : 0m;
        
        var standardDeviation = returns.Any()
            ? (decimal)Math.Sqrt((double)returns.Average(r => (r - averageReturn) * (r - averageReturn)))
            : 0m;

        return standardDeviation != 0m 
            ? (averageReturn - RiskFreeRate) / standardDeviation 
            : 0m;
    }

    private decimal CalculateSortinoRatio(List<Trade> trades)
    {
        var returns = trades.Select(t => (t.RealizedPnL ?? 0) / 10000m).ToList();
        var downwardReturns = returns.Where(r => r < 0).ToList();

        var averageReturn = returns.Any() ? returns.Average() : 0m;
        
        var downwardStandardDeviation = downwardReturns.Any()
            ? (decimal)Math.Sqrt((double)downwardReturns.Average(r => (r - averageReturn) * (r - averageReturn)))
            : 0m;

        return downwardStandardDeviation != 0m 
            ? (averageReturn - RiskFreeRate) / downwardStandardDeviation 
            : 0m;
    }
}
