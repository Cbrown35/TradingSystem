using Newtonsoft.Json;
using WebSocket4Net;
using TradingSystem.Common.Interfaces;
using TradingSystem.Common.Models;
using TradingSystem.RealTrading.Models;

namespace TradingSystem.RealTrading.Services;

public class TradovateAdapter : IExchangeAdapter, IDisposable
{
    private readonly TradovateConfig _config;
    private readonly WebSocket _webSocket;
    private readonly Dictionary<string, decimal> _lastPrices;
    private readonly Dictionary<string, List<Trade>> _openOrders;
    private decimal _accountBalance = 10000m; // Starting with 10k USDT for demo

    public TradovateAdapter(TradovateConfig config)
    {
        _config = config;
        _lastPrices = new Dictionary<string, decimal>();
        _openOrders = new Dictionary<string, List<Trade>>();

        _webSocket = new WebSocket(_config.WebSocketUrl);
        _webSocket.Opened += WebSocket_Opened;
        _webSocket.MessageReceived += WebSocket_MessageReceived;
        _webSocket.Error += WebSocket_Error;
        _webSocket.Closed += WebSocket_Closed;

        ConnectWebSocket();
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
            High = price * 1.001m,
            Low = price * 0.999m,
            Close = price,
            Volume = 1000m
        };
    }

    private async Task<decimal> GetMarketPrice(string symbol)
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

    private void ConnectWebSocket()
    {
        try
        {
            _webSocket.Open();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WebSocket connection error: {ex.Message}");
        }
    }

    private void WebSocket_Opened(object? sender, EventArgs e)
    {
        var authMessage = new
        {
            action = "auth",
            key = _config.ApiKey,
            secret = _config.ApiSecret
        };

        _webSocket.Send(JsonConvert.SerializeObject(authMessage));
    }

    private void WebSocket_MessageReceived(object? sender, MessageReceivedEventArgs e)
    {
        try
        {
            var data = JsonConvert.DeserializeObject<dynamic>(e.Message);
            var messageType = (string)data.type;

            switch (messageType)
            {
                case "price":
                    var symbol = (string)data.symbol;
                    var price = (decimal)data.price;
                    _lastPrices[symbol] = price;
                    break;

                case "order":
                    // Handle order updates
                    break;

                case "error":
                    Console.WriteLine($"WebSocket error: {data.message}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing WebSocket message: {ex.Message}");
        }
    }

    private void WebSocket_Error(object? sender, SuperSocket.ClientEngine.ErrorEventArgs e)
    {
        Console.WriteLine($"WebSocket error: {e.Exception.Message}");
    }

    private void WebSocket_Closed(object? sender, EventArgs e)
    {
        Console.WriteLine("WebSocket connection closed. Attempting to reconnect...");
        Task.Delay(5000).ContinueWith(_ => ConnectWebSocket());
    }

    public void Dispose()
    {
        _webSocket?.Dispose();
    }
}
