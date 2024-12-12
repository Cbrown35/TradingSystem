using TradingSystem.Common.Models;

namespace TradingSystem.Common.Interfaces;

public interface IOrderRepository
{
    /// <summary>
    /// Adds a new order to the repository
    /// </summary>
    Task<Order> AddOrder(Order order);

    /// <summary>
    /// Updates an existing order in the repository
    /// </summary>
    Task<Order> UpdateOrder(Order order);

    /// <summary>
    /// Gets an order by its ID
    /// </summary>
    Task<Order?> GetOrder(string id);

    /// <summary>
    /// Gets an order by its client order ID
    /// </summary>
    Task<Order?> GetOrderByClientId(string clientOrderId);

    /// <summary>
    /// Gets an order by its exchange order ID
    /// </summary>
    Task<Order?> GetOrderByExchangeId(string exchangeOrderId);

    /// <summary>
    /// Gets all open orders, optionally filtered by symbol
    /// </summary>
    Task<List<Order>> GetOpenOrders(string? symbol = null);

    /// <summary>
    /// Gets order history within a specified date range, optionally filtered by symbol and strategy
    /// </summary>
    Task<List<Order>> GetOrderHistory(
        DateTime startDate,
        DateTime endDate,
        string? symbol = null,
        string? strategyName = null);

    /// <summary>
    /// Gets all orders associated with a specific trade
    /// </summary>
    Task<List<Order>> GetOrdersByTradeId(Guid tradeId);

    /// <summary>
    /// Gets orders by their status, optionally filtered by symbol
    /// </summary>
    Task<List<Order>> GetOrdersByStatus(OrderStatus status, string? symbol = null);

    /// <summary>
    /// Gets orders grouped by strategy name for specified strategies
    /// </summary>
    Task<Dictionary<string, List<Order>>> GetOrdersByStrategy(List<string> strategyNames);

    /// <summary>
    /// Gets performance metrics for orders within a specified date range
    /// </summary>
    Task<OrderPerformanceMetrics> GetOrderPerformanceMetrics(
        DateTime startDate,
        DateTime endDate,
        string? symbol = null,
        string? strategyName = null);

    /// <summary>
    /// Gets the most recent orders up to the specified count
    /// </summary>
    Task<List<Order>> GetRecentOrders(int count);

    /// <summary>
    /// Deletes an order from the repository
    /// </summary>
    Task DeleteOrder(string id);
}
