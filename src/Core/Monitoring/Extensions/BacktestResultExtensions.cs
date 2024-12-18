using TradingSystem.Common.Models;

namespace TradingSystem.Core.Monitoring.Extensions;

public static class BacktestResultExtensions
{
    public static List<TimeSpan> GetDataGaps(this BacktestResult result)
    {
        var gaps = new List<TimeSpan>();
        if (result.Trades.Count < 2) return gaps;

        var orderedTrades = result.Trades.OrderBy(t => t.Timestamp).ToList();
        for (int i = 1; i < orderedTrades.Count; i++)
        {
            var gap = orderedTrades[i].Timestamp - orderedTrades[i - 1].Timestamp;
            if (gap > TimeSpan.FromMinutes(5)) // Consider gaps > 5 minutes
            {
                gaps.Add(gap);
            }
        }
        return gaps;
    }

    public static List<string> GetProcessingErrors(this BacktestResult result)
    {
        var errors = new List<string>();
        
        // Check for basic data consistency
        if (result.StartDate >= result.EndDate)
        {
            errors.Add("Invalid date range: Start date is after or equal to end date");
        }

        // Check for trade data consistency
        if (result.TotalTrades != result.WinningTrades + result.LosingTrades)
        {
            errors.Add("Trade count mismatch: Total trades doesn't match sum of winning and losing trades");
        }

        // Check for performance metric consistency
        if (result.WinRate < 0 || result.WinRate > 1)
        {
            errors.Add("Invalid win rate: Must be between 0 and 1");
        }

        if (result.ProfitFactor < 0)
        {
            errors.Add("Invalid profit factor: Must be non-negative");
        }

        return errors;
    }

    public static long GetMemoryUsage(this BacktestResult result)
    {
        // Estimate memory usage based on data size
        long memoryUsage = 0;
        
        // Account for trades
        memoryUsage += result.Trades.Count * 256; // Rough estimate per trade object

        // Account for performance metrics
        memoryUsage += sizeof(decimal) * 10; // Various decimal metrics
        memoryUsage += sizeof(int) * 5; // Various integer metrics

        // Account for symbol performance dictionary
        memoryUsage += result.SymbolPerformance.Count * 64; // Rough estimate per dictionary entry

        return memoryUsage;
    }
}
