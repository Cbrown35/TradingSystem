using Microsoft.Extensions.Logging;
using TradingSystem.Common.Interfaces;
using TradingSystem.Common.Models;

namespace TradingSystem.RealTrading.Services;

public class MarketDataService : IMarketDataService
{
    private readonly IMarketDataRepository _repository;
    private readonly IExchangeAdapter _exchangeAdapter;
    private readonly IMarketDataCacheService _cache;
    private readonly ILogger<MarketDataService> _logger;
    private readonly Dictionary<string, Dictionary<TimeFrame, Action<MarketData>>> _subscribers;

    public MarketDataService(
        IMarketDataRepository repository,
        IExchangeAdapter exchangeAdapter,
        IMarketDataCacheService cache,
        ILogger<MarketDataService> logger)
    {
        _repository = repository;
        _exchangeAdapter = exchangeAdapter;
        _cache = cache;
        _logger = logger;
        _subscribers = new Dictionary<string, Dictionary<TimeFrame, Action<MarketData>>>();
    }

    public async Task<IEnumerable<string>> GetActiveSymbolsAsync()
    {
        try
        {
            _logger.LogDebug("Getting active symbols");
            return await _repository.GetAvailableSymbols();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active symbols");
            throw;
        }
    }

    public async Task<MarketData?> GetLatestMarketDataAsync(string symbol)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                throw new ArgumentException("Symbol cannot be null or empty", nameof(symbol));
            }

            // Try to get from cache first
            var cachedData = _cache.GetLatestData(symbol);
            if (cachedData != null)
            {
                _logger.LogTrace("Returning cached market data for {Symbol}", symbol);
                return cachedData;
            }

            _logger.LogDebug("Fetching latest market data for {Symbol}", symbol);
            var data = await _exchangeAdapter.GetMarketData(symbol);
            if (data != null)
            {
                _cache.SetLatestData(symbol, data);
            }
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest market data for {Symbol}", symbol);
            throw;
        }
    }

    public async Task<IEnumerable<MarketData>> GetHistoricalDataAsync(
        string symbol,
        DateTime startTime,
        DateTime endTime,
        TimeFrame timeFrame)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                throw new ArgumentException("Symbol cannot be null or empty", nameof(symbol));
            }

            if (startTime >= endTime)
            {
                throw new ArgumentException("Start time must be before end time");
            }

            // Try to get from cache first
            var cachedData = _cache.GetHistoricalData(symbol, startTime, endTime, timeFrame);
            if (cachedData != null)
            {
                _logger.LogTrace("Returning cached historical data for {Symbol}", symbol);
                return cachedData;
            }

            _logger.LogDebug("Getting historical data for {Symbol} from {StartTime} to {EndTime}", 
                symbol, startTime, endTime);

            var data = await _repository.GetHistoricalData(symbol, startTime, endTime, timeFrame);
            if (!data.Any())
            {
                _logger.LogInformation("No historical data found in repository for {Symbol}, fetching from exchange", 
                    symbol);

                var marketData = await _exchangeAdapter.GetMarketData(symbol);
                if (marketData != null)
                {
                    await _repository.AddMarketData(marketData);
                    data = new List<MarketData> { marketData };
                }
            }

            // Cache the data if we got any
            if (data.Any())
            {
                _cache.SetHistoricalData(symbol, startTime, endTime, timeFrame, data);
            }

            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting historical data for {Symbol}", symbol);
            throw;
        }
    }

    public async Task<IEnumerable<MarketData>> GetRealtimeDataAsync(string symbol, TimeFrame timeFrame)
    {
        try
        {
            var data = await GetLatestMarketDataAsync(symbol);
            if (data != null)
            {
                var startTime = RoundDownToTimeFrame(data.Timestamp, timeFrame);
                var historicalData = await GetHistoricalDataAsync(symbol, startTime, data.Timestamp, timeFrame);
                return historicalData.Append(data);
            }
            return Enumerable.Empty<MarketData>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting realtime data for {Symbol}", symbol);
            throw;
        }
    }

    public void SubscribeToMarketDataAsync(string symbol, TimeFrame timeFrame, Action<MarketData> callback)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                throw new ArgumentException("Symbol cannot be null or empty", nameof(symbol));
            }

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            _logger.LogInformation("Subscribing to market data for {Symbol} at {TimeFrame}", symbol, timeFrame);

            if (!_subscribers.ContainsKey(symbol))
            {
                _subscribers[symbol] = new Dictionary<TimeFrame, Action<MarketData>>();
            }
            _subscribers[symbol][timeFrame] = callback;

            // Start receiving market data updates
            _ = StartReceivingMarketDataUpdatesAsync(symbol, timeFrame);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to market data for {Symbol}", symbol);
            throw;
        }
    }

    public void UnsubscribeFromMarketDataAsync(string symbol, TimeFrame timeFrame)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                throw new ArgumentException("Symbol cannot be null or empty", nameof(symbol));
            }

            _logger.LogInformation("Unsubscribing from market data for {Symbol} at {TimeFrame}", symbol, timeFrame);

            if (_subscribers.TryGetValue(symbol, out var timeFrames))
            {
                timeFrames.Remove(timeFrame);
                if (!timeFrames.Any())
                {
                    _subscribers.Remove(symbol);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from market data for {Symbol}", symbol);
            throw;
        }
    }

    public async Task<MarketStatistics> GetMarketStatisticsAsync(string symbol, DateTime startTime, DateTime endTime)
    {
        try
        {
            _logger.LogDebug("Getting market statistics for {Symbol}", symbol);
            var data = await GetHistoricalDataAsync(symbol, startTime, endTime, TimeFrame.Day);
            return CalculateStatistics(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting market statistics for {Symbol}", symbol);
            throw;
        }
    }

    public async Task<LiquidityMetrics> GetLiquidityMetricsAsync(string symbol)
    {
        try
        {
            _logger.LogDebug("Getting liquidity metrics for {Symbol}", symbol);

            var data = await GetLatestMarketDataAsync(symbol);
            if (data == null)
            {
                _logger.LogWarning("No market data available for {Symbol}", symbol);
                return new LiquidityMetrics { Symbol = symbol };
            }

            var metrics = new LiquidityMetrics
            {
                Symbol = symbol,
                Timestamp = data.Timestamp,
                BidAskSpread = (data.AskPrice ?? 0) - (data.BidPrice ?? 0),
                MarketDepth = data.Volume,
                TradingVolume = data.Volume,
                OrderBookImbalance = CalculateImbalance(data),
                AverageTradeSize = data.NumberOfTrades > 0 ? data.Volume / data.NumberOfTrades : 0
            };

            // Convert OrderBook to OrderBookLevels dictionary with error handling
            try
            {
                metrics.OrderBookLevels = OrderBookLevel.ToDictionary(data.OrderBook);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error converting order book levels for {Symbol}", symbol);
                metrics.OrderBookLevels = new Dictionary<decimal, decimal>();
            }

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting liquidity metrics for {Symbol}", symbol);
            throw;
        }
    }

    public async Task<Dictionary<string, decimal>> GetCurrentPricesAsync(IEnumerable<string> symbols)
    {
        try
        {
            var prices = new Dictionary<string, decimal>();
            foreach (var symbol in symbols)
            {
                try
                {
                    var data = await GetLatestMarketDataAsync(symbol);
                    if (data != null)
                    {
                        prices[symbol] = data.Close;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error getting price for {Symbol}", symbol);
                    // Continue with other symbols even if one fails
                }
            }
            return prices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current prices");
            throw;
        }
    }

    public async Task<IEnumerable<MarketData>> GetAggregatedDataAsync(
        string symbol,
        TimeFrame timeFrame,
        DateTime startTime,
        DateTime endTime)
    {
        try
        {
            _logger.LogDebug("Getting aggregated data for {Symbol} at {TimeFrame}", symbol, timeFrame);
            var rawData = await GetHistoricalDataAsync(symbol, startTime, endTime, TimeFrame.Minute);
            return AggregateHistoricalData(rawData, timeFrame);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting aggregated data for {Symbol}", symbol);
            throw;
        }
    }

    private async Task StartReceivingMarketDataUpdatesAsync(string symbol, TimeFrame timeFrame)
    {
        while (_subscribers.ContainsKey(symbol) && _subscribers[symbol].ContainsKey(timeFrame))
        {
            try
            {
                var data = await GetLatestMarketDataAsync(symbol);
                if (data != null && _subscribers.TryGetValue(symbol, out var callbacks))
                {
                    if (callbacks.TryGetValue(timeFrame, out var callback))
                    {
                        callback(data);
                        // Invalidate cache after pushing update to subscribers
                        _cache.InvalidateSymbolCache(symbol);
                    }
                }
                await Task.Delay(1000); // Poll every second
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in market data update loop for {Symbol}", symbol);
                await Task.Delay(5000); // Wait longer on error before retrying
            }
        }
    }

    private MarketStatistics CalculateStatistics(IEnumerable<MarketData> data)
    {
        if (!data.Any())
        {
            throw new ArgumentException("No data provided for statistics calculation");
        }

        var stats = new MarketStatistics
        {
            Symbol = data.First().Symbol,
            Period = data.Last().Timestamp - data.First().Timestamp,
            AverageVolume = data.Average(d => d.Volume),
            AveragePrice = data.Average(d => d.Close),
            Volatility = CalculateVolatility(data),
            TrendStrength = CalculateTrendStrength(data),
            HighestPrice = data.Max(d => d.High),
            LowestPrice = data.Min(d => d.Low)
        };

        stats.ActivityByTimeFrame[TimeFrame.Day] = new TradingActivity
        {
            Volume = data.Average(d => d.Volume),
            Volatility = stats.Volatility,
            AverageSpread = data.Average(d => (d.AskPrice ?? 0) - (d.BidPrice ?? 0)),
            TradeCount = data.Sum(d => d.NumberOfTrades)
        };

        return stats;
    }

    private decimal CalculateVolatility(IEnumerable<MarketData> data)
    {
        var returns = new List<decimal>();
        var prices = data.Select(d => d.Close).ToList();
        
        for (int i = 1; i < prices.Count; i++)
        {
            returns.Add((prices[i] - prices[i - 1]) / prices[i - 1]);
        }

        var mean = returns.Average();
        var variance = returns.Sum(r => Math.Pow((double)(r - mean), 2)) / (returns.Count - 1);
        return (decimal)Math.Sqrt(variance);
    }

    private decimal CalculateTrendStrength(IEnumerable<MarketData> data)
    {
        var prices = data.Select(d => d.Close).ToList();
        var upMoves = 0;
        var downMoves = 0;

        for (int i = 1; i < prices.Count; i++)
        {
            if (prices[i] > prices[i - 1]) upMoves++;
            else if (prices[i] < prices[i - 1]) downMoves++;
        }

        var totalMoves = upMoves + downMoves;
        return totalMoves > 0 ? (decimal)Math.Abs(upMoves - downMoves) / totalMoves : 0;
    }

    private decimal CalculateImbalance(MarketData data)
    {
        var buyVolume = data.TakerBuyVolume ?? 0;
        var sellVolume = data.TakerSellVolume ?? 0;
        var totalVolume = buyVolume + sellVolume;
        return totalVolume > 0 ? buyVolume / totalVolume : 0;
    }

    private IEnumerable<MarketData> AggregateHistoricalData(IEnumerable<MarketData> data, TimeFrame timeFrame)
    {
        return data.GroupBy(d => RoundDownToTimeFrame(d.Timestamp, timeFrame))
            .Select(g => new MarketData
            {
                Symbol = g.First().Symbol,
                Timestamp = g.Key,
                Open = g.First().Open,
                High = g.Max(d => d.High),
                Low = g.Min(d => d.Low),
                Close = g.Last().Close,
                Volume = g.Sum(d => d.Volume),
                NumberOfTrades = g.Sum(d => d.NumberOfTrades),
                VWAP = g.Sum(d => d.Volume * d.Close) / g.Sum(d => d.Volume)
            });
    }

    private DateTime RoundDownToTimeFrame(DateTime dt, TimeFrame timeFrame)
    {
        return timeFrame switch
        {
            TimeFrame.Minute => new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0),
            TimeFrame.Hour => new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0),
            TimeFrame.Day => new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0),
            TimeFrame.Week => dt.AddDays(-(int)dt.DayOfWeek).Date,
            TimeFrame.Month => new DateTime(dt.Year, dt.Month, 1),
            _ => dt
        };
    }
}
