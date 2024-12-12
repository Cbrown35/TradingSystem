using TradingSystem.Common.Models;

namespace TradingSystem.Common.Interfaces;

public interface ITradeRepository
{
    /// <summary>
    /// Adds a new trade to the repository
    /// </summary>
    Task<Trade> AddTrade(Trade trade);

    /// <summary>
    /// Updates an existing trade in the repository
    /// </summary>
    Task<Trade> UpdateTrade(Trade trade);

    /// <summary>
    /// Gets a trade by its ID
    /// </summary>
    Task<Trade?> GetTrade(Guid id);

    /// <summary>
    /// Gets all currently open positions
    /// </summary>
    Task<List<Trade>> GetOpenPositions();

    /// <summary>
    /// Gets trade history within a specified date range
    /// </summary>
    Task<List<Trade>> GetTradeHistory(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets all trades for a specific symbol
    /// </summary>
    Task<List<Trade>> GetTradesBySymbol(string symbol);

    /// <summary>
    /// Gets trades grouped by strategy name for specified strategies
    /// </summary>
    Task<Dictionary<string, List<Trade>>> GetTradesByStrategy(List<string> strategyNames);

    /// <summary>
    /// Gets performance metrics for trades within a specified date range
    /// </summary>
    Task<TradePerformanceMetrics> GetPerformanceMetrics(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Gets the most recent trades up to the specified count
    /// </summary>
    Task<List<Trade>> GetRecentTrades(int count);

    /// <summary>
    /// Deletes a trade from the repository
    /// </summary>
    Task DeleteTrade(Guid id);
}
