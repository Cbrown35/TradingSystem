using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradingSystem.Common.Interfaces;
using TradingSystem.Common.Models;
using TradingSystem.Infrastructure.Data;

namespace TradingSystem.Infrastructure.Repositories;

public class TradeRepository : RepositoryBase<Trade>, ITradeRepository
{
    public TradeRepository(
        TradingContext context,
        ILogger<TradeRepository> logger) : base(context, logger)
    {
    }

    public async Task<Trade> AddTrade(Trade trade)
    {
        return await AddAsync(trade);
    }

    public async Task<Trade> UpdateTrade(Trade trade)
    {
        return await UpdateAsync(trade);
    }

    public async Task<Trade?> GetTrade(Guid id)
    {
        return await GetByIdAsync(id);
    }

    public async Task<List<Trade>> GetOpenPositions()
    {
        return await DbSet
            .Where(t => t.Status == TradeStatus.Open)
            .OrderBy(t => t.OpenTime)
            .ToListAsync();
    }

    public async Task<List<Trade>> GetTradeHistory(DateTime startDate, DateTime endDate)
    {
        return await DbSet
            .Where(t => t.OpenTime >= startDate && t.OpenTime <= endDate)
            .OrderByDescending(t => t.OpenTime)
            .ToListAsync();
    }

    public async Task<List<Trade>> GetTradesBySymbol(string symbol)
    {
        return await DbSet
            .Where(t => t.Symbol == symbol)
            .OrderByDescending(t => t.OpenTime)
            .ToListAsync();
    }

    public async Task<Dictionary<string, List<Trade>>> GetTradesByStrategy(List<string> strategyNames)
    {
        var trades = await DbSet
            .Where(t => strategyNames.Contains(t.StrategyName))
            .OrderByDescending(t => t.OpenTime)
            .ToListAsync();

        return trades.GroupBy(t => t.StrategyName)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public async Task<TradePerformanceMetrics> GetPerformanceMetrics(DateTime startDate, DateTime endDate)
    {
        var trades = await DbSet
            .Where(t => t.OpenTime >= startDate && t.OpenTime <= endDate && t.Status == TradeStatus.Closed)
            .OrderBy(t => t.OpenTime)
            .ToListAsync();

        var metrics = new TradePerformanceMetrics
        {
            TotalTrades = trades.Count,
            WinningTrades = trades.Count(t => (t.RealizedPnL ?? 0) > 0),
            LosingTrades = trades.Count(t => (t.RealizedPnL ?? 0) <= 0)
        };

        if (!trades.Any()) return metrics;

        var winningTrades = trades.Where(t => (t.RealizedPnL ?? 0) > 0).ToList();
        var losingTrades = trades.Where(t => (t.RealizedPnL ?? 0) <= 0).ToList();

        // Basic metrics
        metrics.WinRate = (decimal)metrics.WinningTrades / metrics.TotalTrades;
        metrics.AverageWin = winningTrades.Any() ? winningTrades.Average(t => t.RealizedPnL ?? 0) : 0;
        metrics.AverageLoss = losingTrades.Any() ? Math.Abs(losingTrades.Average(t => t.RealizedPnL ?? 0)) : 0;
        metrics.LargestWin = winningTrades.Any() ? winningTrades.Max(t => t.RealizedPnL ?? 0) : 0;
        metrics.LargestLoss = losingTrades.Any() ? Math.Abs(losingTrades.Min(t => t.RealizedPnL ?? 0)) : 0;
        metrics.TotalPnL = trades.Sum(t => t.RealizedPnL ?? 0);

        // Risk metrics
        metrics.ProfitFactor = metrics.AverageLoss != 0 ? metrics.AverageWin / metrics.AverageLoss : 0;
        metrics.MaxDrawdown = CalculateMaxDrawdown(trades);
        metrics.ReturnOnInvestment = CalculateROI(trades);
        metrics.ExpectedValue = (metrics.WinRate * metrics.AverageWin) - ((1 - metrics.WinRate) * metrics.AverageLoss);

        // Time-based metrics
        metrics.AverageHoldingTime = TimeSpan.FromTicks((long)trades
            .Where(t => t.CloseTime.HasValue)
            .Average(t => (t.CloseTime!.Value - t.OpenTime).Ticks));

        // Consecutive trades
        metrics.MaxConsecutiveWins = CalculateMaxConsecutive(trades, true);
        metrics.MaxConsecutiveLosses = CalculateMaxConsecutive(trades, false);

        // Distribution metrics
        metrics.HoldingTimeDistribution = CalculateHoldingTimeDistribution(trades);
        metrics.ProfitDistribution = CalculateProfitDistribution(trades);
        metrics.DayOfWeekPerformance = CalculateDayOfWeekPerformance(trades);
        metrics.HourOfDayPerformance = CalculateHourOfDayPerformance(trades);

        // Performance by category
        metrics.SymbolPerformance = trades.GroupBy(t => t.Symbol)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.RealizedPnL ?? 0));
        metrics.StrategyPerformance = trades.GroupBy(t => t.StrategyName)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.RealizedPnL ?? 0));
        metrics.MonthlyReturns = CalculateMonthlyReturns(trades);

        // Risk-adjusted returns
        metrics.SharpeRatio = CalculateSharpeRatio(trades);

        return metrics;
    }

    public async Task<List<Trade>> GetRecentTrades(int count)
    {
        return await DbSet
            .OrderByDescending(t => t.OpenTime)
            .Take(count)
            .ToListAsync();
    }

    public async Task DeleteTrade(Guid id)
    {
        var trade = await GetByIdAsync(id);
        if (trade != null)
        {
            await DeleteAsync(trade);
        }
    }

    private decimal CalculateMaxDrawdown(List<Trade> trades)
    {
        decimal peak = 0;
        decimal maxDrawdown = 0;
        decimal currentEquity = 10000m;

        foreach (var trade in trades)
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

    private decimal CalculateROI(List<Trade> trades)
    {
        var initialEquity = 10000m;
        var finalEquity = initialEquity + trades.Sum(t => t.RealizedPnL ?? 0);
        return (finalEquity - initialEquity) / initialEquity;
    }

    private int CalculateMaxConsecutive(List<Trade> trades, bool winning)
    {
        int maxConsecutive = 0;
        int currentConsecutive = 0;

        foreach (var trade in trades)
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

    private Dictionary<TimeSpan, decimal> CalculateHoldingTimeDistribution(List<Trade> trades)
    {
        return trades
            .Where(t => t.CloseTime.HasValue)
            .GroupBy(t => TimeSpan.FromHours(Math.Round((t.CloseTime!.Value - t.OpenTime).TotalHours)))
            .ToDictionary(g => g.Key, g => g.Average(t => t.RealizedPnL ?? 0));
    }

    private Dictionary<decimal, int> CalculateProfitDistribution(List<Trade> trades)
    {
        const int buckets = 10;
        var profits = trades.Select(t => t.RealizedPnL ?? 0).ToList();
        var minProfit = profits.Min();
        var maxProfit = profits.Max();
        var bucketSize = (maxProfit - minProfit) / buckets;

        return trades.GroupBy(t => Math.Floor((t.RealizedPnL ?? 0) / bucketSize) * bucketSize)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private Dictionary<DayOfWeek, decimal> CalculateDayOfWeekPerformance(List<Trade> trades)
    {
        return trades.GroupBy(t => t.OpenTime.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.RealizedPnL ?? 0));
    }

    private Dictionary<int, decimal> CalculateHourOfDayPerformance(List<Trade> trades)
    {
        return trades.GroupBy(t => t.OpenTime.Hour)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.RealizedPnL ?? 0));
    }

    private Dictionary<string, decimal> CalculateMonthlyReturns(List<Trade> trades)
    {
        return trades.GroupBy(t => t.OpenTime.ToString("yyyy-MM"))
            .ToDictionary(g => g.Key, g => g.Sum(t => t.RealizedPnL ?? 0));
    }

    private decimal CalculateSharpeRatio(List<Trade> trades)
    {
        var returns = trades.Select(t => (t.RealizedPnL ?? 0) / 10000m).ToList();
        var averageReturn = returns.Average();
        var stdDev = CalculateStdDev(returns);

        const decimal riskFreeRate = 0.02m; // 2% annual risk-free rate
        return stdDev > 0 
            ? (averageReturn - (riskFreeRate / 252)) / stdDev * (decimal)Math.Sqrt(252) 
            : 0;
    }

    private decimal CalculateStdDev(IEnumerable<decimal> values)
    {
        var avg = values.Average();
        var sumOfSquares = values.Sum(v => (v - avg) * (v - avg));
        return values.Count() > 1 
            ? (decimal)Math.Sqrt((double)(sumOfSquares / (values.Count() - 1))) 
            : 0;
    }
}
