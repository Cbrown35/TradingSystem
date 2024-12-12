using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using TradingSystem.Common.Interfaces;
using TradingSystem.Common.Models;
using TradingSystem.RealTrading.Models;

namespace TradingSystem.RealTrading.Services;

public class TradingViewAdapter : IExchangeAdapter, IDisposable
{
    private readonly TradingViewConfig _config;
    private readonly IWebHost _webhookServer;
    private readonly Dictionary<string, decimal> _lastPrices;
    private readonly Dictionary<string, List<Trade>> _openOrders;
    private decimal _accountBalance = 10000m; // Starting with 10k USDT for demo

    public TradingViewAdapter(TradingViewConfig config)
    {
        _config = config;
        _lastPrices = new Dictionary<string, decimal>();
        _openOrders = new Dictionary<string, List<Trade>>();

        // Set up webhook server
        _webhookServer = new WebHostBuilder()
            .UseKestrel()
            .Configure(app =>
            {
                app.Run(async context =>
                {
                    if (context.Request.Path == "/webhook" && context.Request.Method == "POST")
                    {
                        using var reader = new StreamReader(context.Request.Body);
                        var json = await reader.ReadToEndAsync();
                        await HandleWebhook(json);
                        await context.Response.WriteAsync("OK");
                    }
                });
            })
            .UseUrls($"http://localhost:{_config.WebhookPort}")
            .Build();

        _webhookServer.Start();
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

    private async Task HandleWebhook(string json)
    {
        try
        {
            var data = JsonConvert.DeserializeObject<dynamic>(json);
            var symbol = (string)data.symbol;
            var price = (decimal)data.price;
            _lastPrices[symbol] = price;
        }
        catch (Exception ex)
        {
            // Log error
            Console.WriteLine($"Error handling webhook: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _webhookServer?.Dispose();
    }
}
