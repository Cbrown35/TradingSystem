using Microsoft.Extensions.Logging;
using TradingSystem.Common.Interfaces;
using TradingSystem.Common.Models;
using TradingSystem.RealTrading.Models;

namespace TradingSystem.RealTrading.Services;

public class TradovateAdapter : IExchangeAdapter
{
    private readonly TradovateConfig _config;
    private readonly ILogger<TradovateAdapter> _logger;
    private readonly Dictionary<string, decimal> _lastPrices;
    private readonly Dictionary<string, List<Trade>> _openOrders;
    private readonly Dictionary<string, List<OrderBookLevel>> _orderBooks;
    private decimal _accountBalance = 10000m;

    public TradovateAdapter(TradovateConfig config, ILogger<TradovateAdapter> logger)
    {
        _config = config;
        _logger = logger;
        _lastPrices = new Dictionary<string, decimal>();
        _openOrders = new Dictionary<string, List<Trade>>();
        _orderBooks = new Dictionary<string, List<OrderBookLevel>>();

        InitializeTestData();
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
            Direction = isLong ? TradeDirection.Long : TradeDirection.Short,
            Status = TradeStatus.Open,
            OpenTime = DateTime.UtcNow,
            StopLoss = price * (isLong ? 0.98m : 1.02m),
            TakeProfit = price * (isLong ? 1.04m : 0.96m)
        };

        _openOrders[symbol].Add(trade);
        _accountBalance -= quantity * price;

        _logger.LogInformation("Placed {Direction} order for {Symbol}: {Quantity} @ {Price}",
            isLong ? "LONG" : "SHORT", symbol, quantity, price);

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
                var closePrice = GetCurrentPrice(order.Symbol);
                order.RealizedPnL = (closePrice - order.EntryPrice) * order.Quantity;
                _accountBalance += order.RealizedPnL ?? 0;
                orders.Remove(order);

                _logger.LogInformation("Closed order {OrderId} with PnL: {PnL}", orderId, order.RealizedPnL);
                return order;
            }
        }

        throw new InvalidOperationException($"Order {orderId} not found");
    }

    public List<Trade> GetOpenOrders()
    {
        return _openOrders.Values.SelectMany(x => x).ToList();
    }

    public void CancelAllOrders(string symbol)
    {
        if (_openOrders.ContainsKey(symbol))
        {
            _openOrders[symbol].Clear();
            _logger.LogInformation("Cancelled all orders for {Symbol}", symbol);
        }
    }

    public Task<MarketData> GetMarketData(string symbol)
    {
        var price = GetCurrentPrice(symbol);
        return Task.FromResult(new MarketData
        {
            Symbol = symbol,
            Timestamp = DateTime.UtcNow,
            Open = price,
            High = price * 1.001m,
            Low = price * 0.999m,
            Close = price,
            Volume = 1000m,
            BidPrice = price * 0.9995m,
            AskPrice = price * 1.0005m,
            NumberOfTrades = 100
        });
    }

    public IEnumerable<string> GetActiveSymbols()
    {
        return _lastPrices.Keys;
    }

    public Task<MarketData?> GetLatestPrice(string symbol)
    {
        return GetMarketData(symbol);
    }

    public Task<IEnumerable<MarketData>> GetHistoricalData(string symbol, DateTime startTime, DateTime endTime, TimeFrame timeFrame)
    {
        // For demo purposes, generate some historical data
        var data = new List<MarketData>();
        var currentPrice = GetCurrentPrice(symbol);
        var currentTime = startTime;

        while (currentTime <= endTime)
        {
            var random = new Random();
            var change = (decimal)(random.NextDouble() * 0.02 - 0.01);
            currentPrice *= (1 + change);

            data.Add(new MarketData
            {
                Symbol = symbol,
                Timestamp = currentTime,
                Open = currentPrice,
                High = currentPrice * 1.005m,
                Low = currentPrice * 0.995m,
                Close = currentPrice,
                Volume = random.Next(100, 1000),
                NumberOfTrades = random.Next(10, 100)
            });

            currentTime = timeFrame switch
            {
                TimeFrame.Minute => currentTime.AddMinutes(1),
                TimeFrame.Hour => currentTime.AddHours(1),
                TimeFrame.Day => currentTime.AddDays(1),
                TimeFrame.Week => currentTime.AddDays(7),
                TimeFrame.Month => currentTime.AddMonths(1),
                _ => currentTime.AddMinutes(1)
            };
        }

        return Task.FromResult<IEnumerable<MarketData>>(data);
    }

    public void SubscribeToSymbol(string symbol)
    {
        if (!_lastPrices.ContainsKey(symbol))
        {
            _lastPrices[symbol] = 100m; // Default starting price
        }
        _logger.LogInformation("Subscribed to {Symbol}", symbol);
    }

    public void UnsubscribeFromSymbol(string symbol)
    {
        _logger.LogInformation("Unsubscribed from {Symbol}", symbol);
    }

    public OrderBookLevel[] GetOrderBook(string symbol)
    {
        if (!_orderBooks.ContainsKey(symbol))
        {
            GenerateOrderBook(symbol);
        }
        return _orderBooks[symbol].ToArray();
    }

    private decimal GetCurrentPrice(string symbol)
    {
        if (!_lastPrices.ContainsKey(symbol))
        {
            _lastPrices[symbol] = 100m; // Default starting price
        }

        // Simulate small price movements
        var random = new Random();
        var change = (decimal)(random.NextDouble() * 0.002 - 0.001);
        _lastPrices[symbol] *= (1 + change);

        return _lastPrices[symbol];
    }

    private void GenerateOrderBook(string symbol)
    {
        var orderBook = new List<OrderBookLevel>();
        var basePrice = GetCurrentPrice(symbol);
        var now = DateTime.UtcNow;
        var random = new Random();

        // Generate bid levels
        for (int i = 1; i <= 10; i++)
        {
            orderBook.Add(new OrderBookLevel
            {
                Price = basePrice * (1 - i * 0.001m),
                Size = random.Next(1, 10),
                OrderCount = random.Next(1, 5),
                IsBid = true,
                Timestamp = now
            });
        }

        // Generate ask levels
        for (int i = 1; i <= 10; i++)
        {
            orderBook.Add(new OrderBookLevel
            {
                Price = basePrice * (1 + i * 0.001m),
                Size = random.Next(1, 10),
                OrderCount = random.Next(1, 5),
                IsBid = false,
                Timestamp = now
            });
        }

        _orderBooks[symbol] = orderBook;
    }

    private void InitializeTestData()
    {
        var symbols = new[] { "ES", "NQ", "CL", "GC" }; // Common futures symbols
        foreach (var symbol in symbols)
        {
            _lastPrices[symbol] = new Random().Next(1000, 50000);
            GenerateOrderBook(symbol);
            _logger.LogInformation("Initialized test data for {Symbol}", symbol);
        }
    }
}
