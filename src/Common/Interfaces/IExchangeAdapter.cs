using TradingSystem.Common.Models;

namespace TradingSystem.Common.Interfaces;

public interface IExchangeAdapter
{
    Task<decimal> GetAccountBalance(string asset);
    Task<Trade> PlaceOrder(string symbol, decimal quantity, decimal price, bool isLong, OrderType orderType);
    Task<Trade> CloseOrder(string orderId);
    List<Trade> GetOpenOrders();
    void CancelAllOrders(string symbol);
    Task<MarketData> GetMarketData(string symbol);
    IEnumerable<string> GetActiveSymbols();
    Task<MarketData?> GetLatestPrice(string symbol);
    Task<IEnumerable<MarketData>> GetHistoricalData(string symbol, DateTime startTime, DateTime endTime, TimeFrame timeFrame);
    void SubscribeToSymbol(string symbol);
    void UnsubscribeFromSymbol(string symbol);
    OrderBookLevel[] GetOrderBook(string symbol);
}
