using TradingSystem.Common.Interfaces;
using TradingSystem.Common.Models;

namespace TradingSystem.RealTrading.Services;

public class SimulatedExchangeAdapter : IExchangeAdapter
{
    private decimal _accountBalance = 10000m; // Starting with 10k USDT
    private readonly Dictionary<string, List<Trade>> _openOrders;
    private readonly Dictionary<string, decimal> _lastPrices;
    private readonly Random _random;

    public SimulatedExchangeAdapter()
    {
        _openOrders = new Dictionary<string, List<Trade>>();
        _lastPrices = new Dictionary<string, decimal>();
        _random = new Random();
    }

    public Task<decimal> GetAccountBalance(string asset)
    {
        return Task.FromResult(_accountBalance);
    }

    public async Task<Trade> PlaceOrder(string symbol, decimal quantity, decimal price, bool isLong, OrderType orderType)
    {
        if (!_openOrders.ContainsKey(symbol))
        {
            _openOrders[symbol] = new List<Trade>();
        }

        var trade = new Trade
        {
            Id = Guid.NewGuid(),
            Symbol = symbol,
            EntryPrice = price,
            Quantity = quantity,
            OpenTime = DateTime.UtcNow,
            Status = TradeStatus.Open
        };

        _openOrders[symbol].Add(trade);
        _accountBalance -= quantity * price;

        return trade;
    }

    public async Task<Trade> CloseOrder(string orderId)
    {
        foreach (var orders in _openOrders.Values)
        {
            var order = orders.FirstOrDefault(o => o.Id.ToString() == orderId);
            if (order != null)
            {
                order.Status = TradeStatus.Closed;
                order.CloseTime = DateTime.UtcNow;
                var closePrice = await GetMarketPrice(order.Symbol);
                order.RealizedPnL = (closePrice - order.EntryPrice) * order.Quantity;
                _accountBalance += order.RealizedPnL;
                orders.Remove(order);
                return order;
            }
        }

        throw new InvalidOperationException($"Order {orderId} not found");
    }

    public Task<List<Trade>> GetOpenOrders()
    {
        var allOrders = _openOrders.Values.SelectMany(x => x).ToList();
        return Task.FromResult(allOrders);
    }

    public Task CancelAllOrders(string symbol)
    {
        if (_openOrders.ContainsKey(symbol))
        {
            _openOrders[symbol].Clear();
        }
        return Task.CompletedTask;
    }

    public async Task<MarketData> GetMarketData(string symbol)
    {
        var price = await GetMarketPrice(symbol);
        return new MarketData
        {
            Symbol = symbol,
            Timestamp = DateTime.UtcNow,
            Open = price,
            High = price * (1 + (decimal)(_random.NextDouble() * 0.001)),
            Low = price * (1 - (decimal)(_random.NextDouble() * 0.001)),
            Close = price,
            Volume = (decimal)(_random.NextDouble() * 100)
        };
    }

    private Task<decimal> GetMarketPrice(string symbol)
    {
        if (!_lastPrices.ContainsKey(symbol))
        {
            // Initialize with a random price between 10 and 1000
            _lastPrices[symbol] = (decimal)(_random.NextDouble() * 990 + 10);
        }

        // Simulate price movement with random walk
        var change = (decimal)(_random.NextDouble() * 0.002 - 0.001); // +/- 0.1%
        _lastPrices[symbol] *= (1 + change);

        return Task.FromResult(_lastPrices[symbol]);
    }
}
