using TradingSystem.Common.Models;

namespace TradingSystem.Common.Interfaces;

public interface IMarketDataRepository
{
    /// <summary>
    /// Adds a new market data entry to the repository
    /// </summary>
    Task<MarketData> AddMarketData(MarketData marketData);

    /// <summary>
    /// Adds multiple market data entries to the repository
    /// </summary>
    Task<List<MarketData>> AddMarketDataRange(List<MarketData> marketDataList);

    /// <summary>
    /// Gets historical market data for a symbol within a specified date range
    /// </summary>
    Task<List<MarketData>> GetHistoricalData(
        string symbol,
        DateTime startDate,
        DateTime endDate,
        TimeFrame? timeFrame = null);

    /// <summary>
    /// Gets historical market data for multiple symbols within a specified date range
    /// </summary>
    Task<Dictionary<string, List<MarketData>>> GetHistoricalDataForSymbols(
        List<string> symbols,
        DateTime startDate,
        DateTime endDate,
        TimeFrame? timeFrame = null);

    /// <summary>
    /// Gets the latest market data for a symbol
    /// </summary>
    Task<MarketData?> GetLatestMarketData(
        string symbol,
        TimeFrame? timeFrame = null);

    /// <summary>
    /// Gets the latest market data for multiple symbols
    /// </summary>
    Task<Dictionary<string, MarketData>> GetLatestMarketDataForSymbols(
        List<string> symbols,
        TimeFrame? timeFrame = null);

    /// <summary>
    /// Gets market data for a symbol within a specified time range from now
    /// </summary>
    Task<List<MarketData>> GetMarketDataByTimeRange(
        string symbol,
        TimeSpan timeRange,
        TimeFrame? timeFrame = null);

    /// <summary>
    /// Gets all available symbols in the repository
    /// </summary>
    Task<List<string>> GetAvailableSymbols();

    /// <summary>
    /// Gets market statistics for multiple symbols within a specified date range
    /// </summary>
    Task<Dictionary<string, MarketStatistics>> GetMarketStatistics(
        List<string> symbols,
        DateTime startDate,
        DateTime endDate);

    /// <summary>
    /// Gets market data aggregated by a specific time frame
    /// </summary>
    Task<List<MarketData>> GetAggregatedMarketData(
        string symbol,
        DateTime startDate,
        DateTime endDate,
        TimeFrame targetTimeFrame);

    /// <summary>
    /// Gets volume profile for a symbol within a specified date range
    /// </summary>
    Task<Dictionary<decimal, decimal>> GetVolumeProfile(
        string symbol,
        DateTime startDate,
        DateTime endDate);

    /// <summary>
    /// Gets market correlation matrix for specified symbols
    /// </summary>
    Task<Dictionary<string, Dictionary<string, decimal>>> GetCorrelationMatrix(
        List<string> symbols,
        DateTime startDate,
        DateTime endDate);

    /// <summary>
    /// Gets volatility surface for a symbol
    /// </summary>
    Task<Dictionary<TimeSpan, decimal>> GetVolatilitySurface(
        string symbol,
        DateTime startDate,
        DateTime endDate);

    /// <summary>
    /// Gets liquidity analysis for a symbol
    /// </summary>
    Task<Dictionary<DateTime, LiquidityMetrics>> GetLiquidityAnalysis(
        string symbol,
        DateTime startDate,
        DateTime endDate);
}
