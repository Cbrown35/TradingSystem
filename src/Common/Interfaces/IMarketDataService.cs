using TradingSystem.Common.Models;

namespace TradingSystem.Common.Interfaces;

public interface IMarketDataService
{
    /// <summary>
    /// Gets the current market data for a symbol
    /// </summary>
    Task<MarketData> GetMarketData(string symbol);

    /// <summary>
    /// Gets historical market data for a symbol within a date range
    /// </summary>
    Task<List<MarketData>> GetHistoricalData(string symbol, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets current prices for multiple symbols
    /// </summary>
    Task<Dictionary<string, decimal>> GetCurrentPrices(List<string> symbols);

    /// <summary>
    /// Gets comprehensive market statistics for multiple symbols over a specified period
    /// </summary>
    Task<Dictionary<string, MarketStatistics>> GetMarketStatistics(List<string> symbols, TimeSpan period);
}
