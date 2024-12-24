using Microsoft.Extensions.Logging;
using TradingSystem.Common.Interfaces;
using TradingSystem.Common.Models;

namespace TradingSystem.RealTrading.Services;

/// <summary>
/// Simulated exchange adapter for testing trading functionality without connecting to a real exchange.
/// Provides realistic market data simulation and order execution for development and testing.
/// Features:
/// - Simulated price movements
/// - Order tracking and management
/// - PnL calculation
/// - Market data generation
/// - Balance management
/// </summary>
public class SimulatedExchangeAdapter : IExchangeAdapter
{
    private readonly Dictionary<string, decimal> _prices;
    private readonly Dictionary<string, List<MarketData>> _historicalData;
    private readonly Dictionary<string, List<OrderBookLevel>> _orderBooks;
    private readonly List<Trade> _openTrades;
    private readonly ILogger<SimulatedExchangeAdapter> _logger;
    private readonly Random _random;

    public SimulatedExchangeAdapter(ILogger<SimulatedExchangeAdapter> logger)
    {
        _logger = logger;
        _prices = new Dictionary<string, decimal>();
        _historicalData = new Dictionary<string, List<MarketData>>();
        _orderBooks = new Dictionary<string, List<OrderBookLevel>>();
        _openTrades = new List<Trade>();
        _random = new Random();

        InitializeTestData();
    }

    /// <summary>
    /// Get the current balance for a specified asset.
    /// </summary>
    /// <param name="asset">The asset symbol (e.g., USDT)</param>
    /// <returns>Fixed test balance of 10,000 USDT</returns>
    /// <remarks>
    /// In a real implementation, this would:
    /// - Query the exchange API for actual balance
    /// - Handle multiple assets
    /// - Update based on trades
    /// </remarks>
    public Task<decimal> GetAccountBalance(string asset)
    {
        return Task.FromResult(10000m); // Simulated balance
    }

    /// <summary>
    /// Place a new order on the simulated exchange.
    /// </summary>
    /// <param name="symbol">Trading pair symbol</param>
    /// <param name="quantity">Order quantity</param>
    /// <param name="price">Order price</param>
    /// <param name="isLong">True for long positions, false for shorts</param>
    /// <param name="orderType">Market or limit order type</param>
    /// <returns>Created trade object</returns>
    /// <remarks>
    /// Automatically sets:
    /// - Stop loss at 2% against entry
    /// - Take profit at 4% in favor
    /// - Tracks order in _openTrades list
    /// </remarks>
    public Task<Trade> PlaceOrder(string symbol, decimal quantity, decimal price, bool isLong, OrderType orderType)
    {
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

        _openTrades.Add(trade);

        _logger.LogInformation("Placed {Direction} order for {Symbol}: {Quantity} @ {Price}",
            isLong ? "LONG" : "SHORT", symbol, quantity, price);

        return Task.FromResult(trade);
    }

    /// <summary>
    /// Close an existing order and calculate PnL.
    /// </summary>
    /// <param name="orderId">ID of the order to close</param>
    /// <returns>Updated trade with closing details</returns>
    /// <remarks>
    /// - Generates random PnL for simulation
    /// - Updates trade status to Closed
    /// - Records closing time
    /// - Removes from open trades list
    /// </remarks>
    public Task<Trade> CloseOrder(string orderId)
    {
        var trade = _openTrades.FirstOrDefault(t => t.Id.ToString() == orderId);
        if (trade != null)
        {
            trade.Status = TradeStatus.Closed;
            trade.CloseTime = DateTime.UtcNow;
            trade.RealizedPnL = _random.Next(-100, 100);
            _openTrades.Remove(trade);
        }
        else
        {
            trade = new Trade
            {
                Id = Guid.Parse(orderId),
                Status = TradeStatus.Closed,
                CloseTime = DateTime.UtcNow,
                RealizedPnL = _random.Next(-100, 100)
            };
        }

        _logger.LogInformation("Closed order {OrderId} with PnL: {PnL}", orderId, trade.RealizedPnL);
        return Task.FromResult(trade);
    }

    /// <summary>
    /// Get all currently open orders.
    /// </summary>
    /// <returns>List of open trades</returns>
    /// <remarks>
    /// Tracks orders placed through PlaceOrder until they are closed.
    /// Each order maintains full trade details including:
    /// - Entry price and quantity
    /// - Direction (long/short)
    /// - Stop loss and take profit levels
    /// </remarks>
    public List<Trade> GetOpenOrders()
    {
        return _openTrades.ToList();
    }

    public void CancelAllOrders(string symbol)
    {
        _logger.LogInformation("Cancelled all orders for {Symbol}", symbol);
    }

    /// <summary>
    /// Get current market data for a symbol.
    /// </summary>
    /// <param name="symbol">Trading pair symbol</param>
    /// <returns>Simulated market data</returns>
    /// <remarks>
    /// Generates realistic market data with:
    /// - Base price from _prices dictionary
    /// - Small price variations (±0.1%)
    /// - Simulated volume
    /// - Bid/Ask spread
    /// </remarks>
    public Task<MarketData> GetMarketData(string symbol)
    {
        if (!_prices.ContainsKey(symbol))
        {
            return Task.FromResult<MarketData>(null);
        }

        var price = _prices[symbol];
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
        return _prices.Keys;
    }

    public Task<MarketData?> GetLatestPrice(string symbol)
    {
        return GetMarketData(symbol);
    }

    public Task<IEnumerable<MarketData>> GetHistoricalData(string symbol, DateTime startTime, DateTime endTime, TimeFrame timeFrame)
    {
        if (!_historicalData.ContainsKey(symbol))
        {
            return Task.FromResult<IEnumerable<MarketData>>(new List<MarketData>());
        }

        return Task.FromResult<IEnumerable<MarketData>>(
            _historicalData[symbol].Where(d => d.Timestamp >= startTime && d.Timestamp <= endTime));
    }

    public void SubscribeToSymbol(string symbol)
    {
        if (!_prices.ContainsKey(symbol))
        {
            _prices[symbol] = 100m; // Default price
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

    /// <summary>
    /// Initialize test data for supported trading pairs.
    /// </summary>
    /// <remarks>
    /// Sets up:
    /// - Initial prices for BTCUSD, ETHUSD, XRPUSD
    /// - Historical price data
    /// - Order book depth
    /// - Logging of initialization
    /// </remarks>
    private void InitializeTestData()
    {
        var symbols = new[] { "BTCUSD", "ETHUSD", "XRPUSD" };
        foreach (var symbol in symbols)
        {
            _prices[symbol] = _random.Next(1000, 50000);
            GenerateHistoricalData(symbol);
            GenerateOrderBook(symbol);
            _logger.LogInformation("Initialized test data for {Symbol}", symbol);
        }
    }

    /// <summary>
    /// Generate historical price data for backtesting.
    /// </summary>
    /// <param name="symbol">Trading pair symbol</param>
    /// <remarks>
    /// Creates 30 days of hourly data with:
    /// - Random price movements (±2%)
    /// - Volume variations
    /// - Trade count simulation
    /// - OHLCV data structure
    /// </remarks>
    private void GenerateHistoricalData(string symbol)
    {
        var data = new List<MarketData>();
        var basePrice = _prices[symbol];
        var startTime = DateTime.UtcNow.AddDays(-30);

        for (int i = 0; i < 30 * 24; i++) // 30 days of hourly data
        {
            var price = basePrice * (1 + ((decimal)_random.NextDouble() - 0.5m) * 0.02m);
            data.Add(new MarketData
            {
                Symbol = symbol,
                Timestamp = startTime.AddHours(i),
                Open = price,
                High = price * 1.005m,
                Low = price * 0.995m,
                Close = price,
                Volume = _random.Next(100, 1000),
                NumberOfTrades = _random.Next(10, 100)
            });
        }

        _historicalData[symbol] = data;
    }

    /// <summary>
    /// Generate simulated order book for market depth analysis.
    /// </summary>
    /// <param name="symbol">Trading pair symbol</param>
    /// <remarks>
    /// Creates realistic order book with:
    /// - 10 levels each side
    /// - Price steps of 0.1%
    /// - Random size and order count
    /// - Bid/Ask spread simulation
    /// </remarks>
    private void GenerateOrderBook(string symbol)
    {
        var orderBook = new List<OrderBookLevel>();
        var basePrice = _prices[symbol];
        var now = DateTime.UtcNow;

        // Generate bid levels
        for (int i = 1; i <= 10; i++)
        {
            orderBook.Add(new OrderBookLevel
            {
                Price = basePrice * (1 - i * 0.001m),
                Size = _random.Next(1, 10),
                OrderCount = _random.Next(1, 5),
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
                Size = _random.Next(1, 10),
                OrderCount = _random.Next(1, 5),
                IsBid = false,
                Timestamp = now
            });
        }

        _orderBooks[symbol] = orderBook;
    }
}
