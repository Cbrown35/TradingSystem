using TradingSystem.Common.Models;

namespace TradingSystem.Common.Interfaces;

public interface IMarketDataService
{
    Task<IEnumerable<string>> GetActiveSymbolsAsync();
    Task<MarketData?> GetLatestMarketDataAsync(string symbol);
    Task<IEnumerable<MarketData>> GetHistoricalDataAsync(string symbol, DateTime startTime, DateTime endTime, TimeFrame timeFrame);
    Task<IEnumerable<MarketData>> GetRealtimeDataAsync(string symbol, TimeFrame timeFrame);
    void SubscribeToMarketDataAsync(string symbol, TimeFrame timeFrame, Action<MarketData> callback);
    void UnsubscribeFromMarketDataAsync(string symbol, TimeFrame timeFrame);
    Task<MarketStatistics> GetMarketStatisticsAsync(string symbol, DateTime startTime, DateTime endTime);
    Task<LiquidityMetrics> GetLiquidityMetricsAsync(string symbol);
    Task<Dictionary<string, decimal>> GetCurrentPricesAsync(IEnumerable<string> symbols);
    Task<IEnumerable<MarketData>> GetAggregatedDataAsync(string symbol, TimeFrame timeFrame, DateTime startTime, DateTime endTime);
}
