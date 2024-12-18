namespace TradingSystem.RealTrading.Configuration;

public class MarketDataCacheConfig
{
    public TimeSpan LatestDataCacheDuration { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan HistoricalDataCacheDuration { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxCacheItems { get; set; } = 1000;
    public bool EnableCaching { get; set; } = true;
}
