using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradingSystem.Common.Interfaces;
using TradingSystem.Common.Models;
using TradingSystem.Infrastructure.Data;

namespace TradingSystem.Infrastructure.Repositories;

public class MarketDataRepository : RepositoryBase<MarketData>, IMarketDataRepository
{
    public MarketDataRepository(
        TradingContext context,
        ILogger<MarketDataRepository> logger) : base(context, logger)
    {
    }

    public async Task<MarketData> AddMarketData(MarketData marketData)
    {
        return await AddAsync(marketData);
    }

    public async Task<List<MarketData>> AddMarketDataRange(List<MarketData> marketDataList)
    {
        await BulkInsertAsync(marketDataList);
        return marketDataList;
    }

    public async Task<List<MarketData>> GetHistoricalData(
        string symbol,
        DateTime startDate,
        DateTime endDate,
        TimeFrame? timeFrame = null)
    {
        var query = DbSet.AsQueryable()
            .Where(m => m.Symbol == symbol && 
                       m.Timestamp >= startDate && 
                       m.Timestamp <= endDate);

        if (timeFrame.HasValue)
        {
            query = query.Where(m => m.Interval == timeFrame.Value);
        }

        return await query
            .OrderBy(m => m.Timestamp)
            .ToListAsync();
    }

    public async Task<Dictionary<string, List<MarketData>>> GetHistoricalDataForSymbols(
        List<string> symbols,
        DateTime startDate,
        DateTime endDate,
        TimeFrame? timeFrame = null)
    {
        var query = DbSet.AsQueryable()
            .Where(m => symbols.Contains(m.Symbol) && 
                       m.Timestamp >= startDate && 
                       m.Timestamp <= endDate);

        if (timeFrame.HasValue)
        {
            query = query.Where(m => m.Interval == timeFrame.Value);
        }

        var data = await query
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

        return data.GroupBy(m => m.Symbol)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public async Task<MarketData?> GetLatestMarketData(string symbol, TimeFrame? timeFrame = null)
    {
        var query = DbSet.AsQueryable()
            .Where(m => m.Symbol == symbol);

        if (timeFrame.HasValue)
        {
            query = query.Where(m => m.Interval == timeFrame.Value);
        }

        return await query
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefaultAsync();
    }

    public async Task<Dictionary<string, MarketData>> GetLatestMarketDataForSymbols(
        List<string> symbols,
        TimeFrame? timeFrame = null)
    {
        var query = DbSet.AsQueryable()
            .Where(m => symbols.Contains(m.Symbol));

        if (timeFrame.HasValue)
        {
            query = query.Where(m => m.Interval == timeFrame.Value);
        }

        var latestTimestamps = await query
            .GroupBy(m => m.Symbol)
            .Select(g => new
            {
                Symbol = g.Key,
                LatestTimestamp = g.Max(m => m.Timestamp)
            })
            .ToListAsync();

        var result = new Dictionary<string, MarketData>();

        foreach (var timestamp in latestTimestamps)
        {
            var data = await query
                .Where(m => m.Symbol == timestamp.Symbol && 
                           m.Timestamp == timestamp.LatestTimestamp)
                .FirstOrDefaultAsync();

            if (data != null)
            {
                result[timestamp.Symbol] = data;
            }
        }

        return result;
    }

    public async Task<List<MarketData>> GetMarketDataByTimeRange(
        string symbol,
        TimeSpan timeRange,
        TimeFrame? timeFrame = null)
    {
        var endDate = DateTime.UtcNow;
        var startDate = endDate - timeRange;

        return await GetHistoricalData(symbol, startDate, endDate, timeFrame);
    }

    public async Task<List<string>> GetAvailableSymbols()
    {
        return await DbSet
            .Select(m => m.Symbol)
            .Distinct()
            .ToListAsync();
    }

    public async Task<Dictionary<string, MarketStatistics>> GetMarketStatistics(
        List<string> symbols,
        DateTime startDate,
        DateTime endDate)
    {
        var data = await GetHistoricalDataForSymbols(symbols, startDate, endDate);
        var result = new Dictionary<string, MarketStatistics>();

        foreach (var (symbol, marketData) in data)
        {
            var stats = new MarketStatistics
            {
                Symbol = symbol,
                Period = endDate - startDate,
                AverageVolume = marketData.Average(m => m.Volume),
                AveragePrice = marketData.Average(m => m.Close),
                HighestPrice = marketData.Max(m => m.High),
                LowestPrice = marketData.Min(m => m.Low),
                Volatility = CalculateVolatility(marketData),
                TrendStrength = CalculateTrendStrength(marketData)
            };

            result[symbol] = stats;
        }

        return result;
    }

    public async Task<List<MarketData>> GetAggregatedMarketData(
        string symbol,
        DateTime startDate,
        DateTime endDate,
        TimeFrame targetTimeFrame)
    {
        var data = await GetHistoricalData(symbol, startDate, endDate);
        return AggregateMarketData(data, targetTimeFrame);
    }

    public async Task<Dictionary<decimal, decimal>> GetVolumeProfile(
        string symbol,
        DateTime startDate,
        DateTime endDate)
    {
        var data = await GetHistoricalData(symbol, startDate, endDate);
        const int pricelevels = 50;

        var minPrice = data.Min(d => d.Low);
        var maxPrice = data.Max(d => d.High);
        var priceRange = maxPrice - minPrice;
        var levelSize = priceRange / pricelevels;

        return data
            .SelectMany(d => new[] 
            { 
                new { Price = d.Low, Volume = d.Volume * 0.5m },
                new { Price = d.High, Volume = d.Volume * 0.5m }
            })
            .GroupBy(p => Math.Floor((p.Price - minPrice) / levelSize) * levelSize + minPrice)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Volume));
    }

    public async Task<Dictionary<string, Dictionary<string, decimal>>> GetCorrelationMatrix(
        List<string> symbols,
        DateTime startDate,
        DateTime endDate)
    {
        var data = await GetHistoricalDataForSymbols(symbols, startDate, endDate);
        var returns = new Dictionary<string, List<decimal>>();

        // Calculate returns for each symbol
        foreach (var (symbol, marketData) in data)
        {
            returns[symbol] = new List<decimal>();
            for (int i = 1; i < marketData.Count; i++)
            {
                var ret = (marketData[i].Close - marketData[i - 1].Close) / marketData[i - 1].Close;
                returns[symbol].Add(ret);
            }
        }

        var correlations = new Dictionary<string, Dictionary<string, decimal>>();

        foreach (var symbol1 in symbols)
        {
            correlations[symbol1] = new Dictionary<string, decimal>();
            foreach (var symbol2 in symbols)
            {
                correlations[symbol1][symbol2] = CalculateCorrelation(returns[symbol1], returns[symbol2]);
            }
        }

        return correlations;
    }

    public async Task<Dictionary<TimeSpan, decimal>> GetVolatilitySurface(
        string symbol,
        DateTime startDate,
        DateTime endDate)
    {
        var data = await GetHistoricalData(symbol, startDate, endDate);
        var timeframes = new[] { 1, 5, 15, 30, 60, 240, 1440 }; // minutes
        var surface = new Dictionary<TimeSpan, decimal>();

        foreach (var minutes in timeframes)
        {
            var timeframe = TimeSpan.FromMinutes(minutes);
            var volatility = CalculateVolatilityForTimeframe(data, timeframe);
            surface[timeframe] = volatility;
        }

        return surface;
    }

    public async Task<Dictionary<DateTime, LiquidityMetrics>> GetLiquidityAnalysis(
        string symbol,
        DateTime startDate,
        DateTime endDate)
    {
        var data = await GetHistoricalData(symbol, startDate, endDate);
        var result = new Dictionary<DateTime, LiquidityMetrics>();

        foreach (var marketData in data)
        {
            var metrics = new LiquidityMetrics
            {
                Symbol = symbol,
                Timestamp = marketData.Timestamp,
                BidAskSpread = marketData.AskPrice!.Value - marketData.BidPrice!.Value,
                MarketDepth = CalculateMarketDepth(marketData),
                TradingVolume = marketData.Volume,
                AverageTradeSize = marketData.Volume / marketData.NumberOfTrades
            };

            result[marketData.Timestamp] = metrics;
        }

        return result;
    }

    private List<MarketData> AggregateMarketData(List<MarketData> data, TimeFrame targetTimeFrame)
    {
        var groupedData = data.GroupBy(d => new DateTime(
            d.Timestamp.Year,
            d.Timestamp.Month,
            d.Timestamp.Day,
            d.Timestamp.Hour,
            d.Timestamp.Minute - (d.Timestamp.Minute % targetTimeFrame.ToTimeSpan().Minutes),
            0
        ));

        return groupedData.Select(g => new MarketData
        {
            Symbol = g.First().Symbol,
            Timestamp = g.Key,
            Open = g.First().Open,
            High = g.Max(d => d.High),
            Low = g.Min(d => d.Low),
            Close = g.Last().Close,
            Volume = g.Sum(d => d.Volume),
            Interval = targetTimeFrame
        }).ToList();
    }

    private decimal CalculateVolatility(List<MarketData> data)
    {
        var returns = new List<decimal>();
        for (int i = 1; i < data.Count; i++)
        {
            var dailyReturn = (data[i].Close - data[i - 1].Close) / data[i - 1].Close;
            returns.Add(dailyReturn);
        }

        var mean = returns.Average();
        var sumSquaredDiff = returns.Sum(r => (r - mean) * (r - mean));
        var variance = sumSquaredDiff / (returns.Count - 1);
        var stdDev = (decimal)Math.Sqrt((double)variance);

        return stdDev * (decimal)Math.Sqrt(252); // Annualized volatility
    }

    private decimal CalculateTrendStrength(List<MarketData> data)
    {
        var prices = data.Select(d => d.Close).ToList();
        var n = prices.Count;
        var x = Enumerable.Range(0, n).Select(i => (decimal)i).ToList();
        var xy = x.Zip(prices, (a, b) => a * b).Sum();
        var xx = x.Sum(a => a * a);
        var sumX = x.Sum();
        var sumY = prices.Sum();

        var slope = (n * xy - sumX * sumY) / (n * xx - sumX * sumX);
        var intercept = (sumY - slope * sumX) / n;
        var yHat = x.Select(a => slope * a + intercept).ToList();

        var ssReg = yHat.Zip(prices, (h, p) => (h - prices.Average()) * (h - prices.Average())).Sum();
        var ssTot = prices.Select(p => (p - prices.Average()) * (p - prices.Average())).Sum();

        return ssReg / ssTot; // R-squared value
    }

    private decimal CalculateCorrelation(List<decimal> x, List<decimal> y)
    {
        var n = Math.Min(x.Count, y.Count);
        var meanX = x.Take(n).Average();
        var meanY = y.Take(n).Average();

        var sumXY = x.Take(n).Zip(y.Take(n), (a, b) => (a - meanX) * (b - meanY)).Sum();
        var sumXX = x.Take(n).Sum(a => (a - meanX) * (a - meanX));
        var sumYY = y.Take(n).Sum(b => (b - meanY) * (b - meanY));

        return sumXY / (decimal)Math.Sqrt((double)(sumXX * sumYY));
    }

    private decimal CalculateVolatilityForTimeframe(List<MarketData> data, TimeSpan timeframe)
    {
        var returns = new List<decimal>();
        var groupedData = data.GroupBy(d => new DateTime(
            d.Timestamp.Year,
            d.Timestamp.Month,
            d.Timestamp.Day,
            d.Timestamp.Hour,
            d.Timestamp.Minute - (d.Timestamp.Minute % timeframe.Minutes),
            0
        )).Select(g => g.Last()).ToList();

        for (int i = 1; i < groupedData.Count; i++)
        {
            var ret = (groupedData[i].Close - groupedData[i - 1].Close) / groupedData[i - 1].Close;
            returns.Ad
