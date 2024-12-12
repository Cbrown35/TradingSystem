using TradingSystem.Common.Interfaces;
using TradingSystem.Common.Models;

namespace TradingSystem.RealTrading.Services;

public class MarketDataService : IMarketDataService
{
    private readonly IExchangeAdapter _exchangeAdapter;
    private readonly Dictionary<string, List<MarketData>> _historicalDataCache;
    private readonly Dictionary<string, DateTime> _lastUpdateTime;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public MarketDataService(IExchangeAdapter exchangeAdapter)
    {
        _exchangeAdapter = exchangeAdapter;
        _historicalDataCache = new Dictionary<string, List<MarketData>>();
        _lastUpdateTime = new Dictionary<string, DateTime>();
    }

    public async Task<MarketData> GetMarketData(string symbol)
    {
        return await _exchangeAdapter.GetMarketData(symbol);
    }

    public async Task<List<MarketData>> GetHistoricalData(string symbol, DateTime startDate, DateTime endDate)
    {
        var cacheKey = $"{symbol}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";

        if (_historicalDataCache.ContainsKey(cacheKey) && 
            _lastUpdateTime.ContainsKey(cacheKey) &&
            DateTime.UtcNow - _lastUpdateTime[cacheKey] < _cacheExpiration)
        {
            return _historicalDataCache[cacheKey];
        }

        var data = await SimulateHistoricalData(symbol, startDate, endDate);
        _historicalDataCache[cacheKey] = data;
        _lastUpdateTime[cacheKey] = DateTime.UtcNow;

        return data;
    }

    public async Task<Dictionary<string, decimal>> GetCurrentPrices(List<string> symbols)
    {
        var prices = new Dictionary<string, decimal>();

        foreach (var symbol in symbols)
        {
            var marketData = await GetMarketData(symbol);
            prices[symbol] = marketData.Close;
        }

        return prices;
    }

    public async Task<Dictionary<string, MarketStatistics>> GetMarketStatistics(List<string> symbols, TimeSpan period)
    {
        var statistics = new Dictionary<string, MarketStatistics>();
        var endDate = DateTime.UtcNow;
        var startDate = endDate - period;

        foreach (var symbol in symbols)
        {
            var historicalData = await GetHistoricalData(symbol, startDate, endDate);
            statistics[symbol] = await CalculateMarketStatistics(symbol, historicalData, period);
        }

        return statistics;
    }

    private async Task<MarketStatistics> CalculateMarketStatistics(string symbol, List<MarketData> data, TimeSpan period)
    {
        var stats = new MarketStatistics
        {
            Symbol = symbol,
            Period = period,
            AverageVolume = data.Average(d => d.Volume),
            AveragePrice = data.Average(d => d.Close),
            HighestPrice = data.Max(d => d.High),
            LowestPrice = data.Min(d => d.Low)
        };

        // Calculate ranges
        stats.DailyRange = CalculateAverageRange(data, TimeSpan.FromDays(1));
        stats.WeeklyRange = CalculateAverageRange(data, TimeSpan.FromDays(7));
        stats.MonthlyRange = CalculateAverageRange(data, TimeSpan.FromDays(30));

        // Calculate technical indicators
        stats.Volatility = CalculateVolatility(data);
        stats.TrendStrength = CalculateTrendStrength(data);
        stats.AverageTrueRange = CalculateATR(data, 14);
        stats.RelativeStrengthIndex = CalculateRSI(data, 14);
        stats.BollingerBandWidth = CalculateBollingerBandWidth(data, 20, 2);

        // Calculate activity by timeframe
        stats.ActivityByTimeFrame = new Dictionary<TimeFrame, TradingActivity>
        {
            [TimeFrame.Hour] = CalculateActivity(data, TimeSpan.FromHours(1)),
            [TimeFrame.Day] = CalculateActivity(data, TimeSpan.FromDays(1)),
            [TimeFrame.Week] = CalculateActivity(data, TimeSpan.FromDays(7))
        };

        return stats;
    }

    private decimal CalculateAverageRange(List<MarketData> data, TimeSpan period)
    {
        var groupedData = data.GroupBy(d => d.Timestamp.Date);
        var ranges = groupedData.Select(g => g.Max(d => d.High) - g.Min(d => d.Low));
        return ranges.Average();
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

    private decimal CalculateATR(List<MarketData> data, int period)
    {
        var trueRanges = new List<decimal>();
        for (int i = 1; i < data.Count; i++)
        {
            var tr1 = data[i].High - data[i].Low;
            var tr2 = Math.Abs(data[i].High - data[i - 1].Close);
            var tr3 = Math.Abs(data[i].Low - data[i - 1].Close);
            trueRanges.Add(Math.Max(tr1, Math.Max(tr2, tr3)));
        }

        return trueRanges.TakeLast(period).Average();
    }

    private decimal CalculateRSI(List<MarketData> data, int period)
    {
        var gains = new List<decimal>();
        var losses = new List<decimal>();

        for (int i = 1; i < data.Count; i++)
        {
            var change = data[i].Close - data[i - 1].Close;
            if (change >= 0)
            {
                gains.Add(change);
                losses.Add(0);
            }
            else
            {
                gains.Add(0);
                losses.Add(-change);
            }
        }

        var avgGain = gains.TakeLast(period).Average();
        var avgLoss = losses.TakeLast(period).Average();

        if (avgLoss == 0) return 100;
        var rs = avgGain / avgLoss;
        return 100 - (100 / (1 + rs));
    }

    private decimal CalculateBollingerBandWidth(List<MarketData> data, int period, decimal multiplier)
    {
        var prices = data.Select(d => d.Close).ToList();
        var sma = prices.TakeLast(period).Average();
        var sumSquaredDiff = prices.TakeLast(period).Sum(p => (p - sma) * (p - sma));
        var stdDev = (decimal)Math.Sqrt((double)(sumSquaredDiff / period));
        var upperBand = sma + (multiplier * stdDev);
        var lowerBand = sma - (multiplier * stdDev);

        return (upperBand - lowerBand) / sma * 100;
    }

    private TradingActivity CalculateActivity(List<MarketData> data, TimeSpan timeframe)
    {
        var groupedData = data.GroupBy(d => new DateTime(
            d.Timestamp.Year,
            d.Timestamp.Month,
            d.Timestamp.Day,
            timeframe == TimeSpan.FromHours(1) ? d.Timestamp.Hour : 0,
            0,
            0
        ));

        return new TradingActivity
        {
            Volume = groupedData.Average(g => g.Sum(d => d.Volume)),
            Volatility = CalculateVolatility(data),
            AverageSpread = data.Average(d => d.High - d.Low),
            TradeCount = data.Count,
            LargeOrderCount = data.Count(d => d.Volume > data.Average(x => x.Volume) * 2)
        };
    }

    private async Task<List<MarketData>> SimulateHistoricalData(string symbol, DateTime startDate, DateTime endDate)
    {
        var data = new List<MarketData>();
        var random = new Random();
        var currentPrice = 100m;
        var currentTime = startDate;

        while (currentTime <= endDate)
        {
            if (currentTime.DayOfWeek != DayOfWeek.Saturday && currentTime.DayOfWeek != DayOfWeek.Sunday)
            {
                var change = (decimal)(random.NextDouble() * 0.02 - 0.01);
                currentPrice *= (1 + change);

                var high = currentPrice * (1 + (decimal)(random.NextDouble() * 0.005));
                var low = currentPrice * (1 - (decimal)(random.NextDouble() * 0.005));
                var open = low + (high - low) * (decimal)random.NextDouble();
                var volume = random.Next(1000, 10000);

                data.Add(new MarketData
                {
                    Symbol = symbol,
                    Timestamp = currentTime,
                    Open = open,
                    High = high,
                    Low = low,
                    Close = currentPrice,
                    Volume = volume
                });
            }

            currentTime = currentTime.AddDays(1);
        }

        return data;
    }
}
