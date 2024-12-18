using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Common.Models;
using TradingSystem.RealTrading.Configuration;

namespace TradingSystem.RealTrading.Services;

public interface IMarketDataCacheService
{
    MarketData? GetLatestData(string symbol);
    void SetLatestData(string symbol, MarketData data);
    IEnumerable<MarketData>? GetHistoricalData(string symbol, DateTime startTime, DateTime endTime, TimeFrame timeFrame);
    void SetHistoricalData(string symbol, DateTime startTime, DateTime endTime, TimeFrame timeFrame, IEnumerable<MarketData> data);
    void InvalidateSymbolCache(string symbol);
    void InvalidateAllCache();
}

public class MarketDataCacheService : IMarketDataCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MarketDataCacheService> _logger;
    private readonly MarketDataCacheConfig _config;

    public MarketDataCacheService(
        IMemoryCache cache,
        ILogger<MarketDataCacheService> logger,
        IOptions<MarketDataCacheConfig> config)
    {
        _cache = cache;
        _logger = logger;
        _config = config.Value;
    }

    public MarketData? GetLatestData(string symbol)
    {
        if (!_config.EnableCaching) return null;

        var cacheKey = GetLatestDataCacheKey(symbol);
        if (_cache.TryGetValue(cacheKey, out MarketData? data))
        {
            _logger.LogTrace("Cache hit for latest data: {Symbol}", symbol);
            return data;
        }

        _logger.LogTrace("Cache miss for latest data: {Symbol}", symbol);
        return null;
    }

    public void SetLatestData(string symbol, MarketData data)
    {
        if (!_config.EnableCaching) return;

        var cacheKey = GetLatestDataCacheKey(symbol);
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(_config.LatestDataCacheDuration)
            .SetSize(1);

        _cache.Set(cacheKey, data, cacheOptions);
        _logger.LogTrace("Cached latest data for {Symbol}", symbol);
    }

    public IEnumerable<MarketData>? GetHistoricalData(string symbol, DateTime startTime, DateTime endTime, TimeFrame timeFrame)
    {
        if (!_config.EnableCaching) return null;

        var cacheKey = GetHistoricalDataCacheKey(symbol, startTime, endTime, timeFrame);
        if (_cache.TryGetValue(cacheKey, out IEnumerable<MarketData>? data))
        {
            _logger.LogTrace("Cache hit for historical data: {Symbol} ({StartTime} to {EndTime})", 
                symbol, startTime, endTime);
            return data;
        }

        _logger.LogTrace("Cache miss for historical data: {Symbol} ({StartTime} to {EndTime})", 
            symbol, startTime, endTime);
        return null;
    }

    public void SetHistoricalData(string symbol, DateTime startTime, DateTime endTime, TimeFrame timeFrame, IEnumerable<MarketData> data)
    {
        if (!_config.EnableCaching) return;

        var cacheKey = GetHistoricalDataCacheKey(symbol, startTime, endTime, timeFrame);
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(_config.HistoricalDataCacheDuration)
            .SetSize(1);

        _cache.Set(cacheKey, data.ToList(), cacheOptions);
        _logger.LogTrace("Cached historical data for {Symbol} ({StartTime} to {EndTime})", 
            symbol, startTime, endTime);
    }

    public void InvalidateSymbolCache(string symbol)
    {
        if (!_config.EnableCaching) return;

        var latestDataKey = GetLatestDataCacheKey(symbol);
        _cache.Remove(latestDataKey);

        // Note: Historical data cache keys contain timestamps, so we can't easily remove them all.
        // They will expire naturally based on the cache duration.
        _logger.LogInformation("Invalidated cache for {Symbol}", symbol);
    }

    public void InvalidateAllCache()
    {
        if (_cache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0);
            _logger.LogInformation("Invalidated all market data cache");
        }
    }

    private static string GetLatestDataCacheKey(string symbol)
        => $"marketdata:latest:{symbol.ToLowerInvariant()}";

    private static string GetHistoricalDataCacheKey(string symbol, DateTime startTime, DateTime endTime, TimeFrame timeFrame)
        => $"marketdata:historical:{symbol.ToLowerInvariant()}:{startTime:yyyyMMddHHmm}:{endTime:yyyyMMddHHmm}:{timeFrame}";
}
