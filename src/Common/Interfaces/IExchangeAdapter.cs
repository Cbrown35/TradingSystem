using TradingSystem.Common.Models;

namespace TradingSystem.Common.Interfaces;

public interface IExchangeAdapter
{
    Task<decimal> GetAccountBalance(string asset);
    Task<Trade> PlaceOrder(string symbol, decimal quantity, decimal price, bool isLong, OrderType orderType);
    Task<Trade> CloseOrder(string orderId);
    Task<List<Trade>> GetOpenOrders();
    Task CancelAllOrders(string symbol);
    Task<MarketData> GetMarketData(string symbol);
}
