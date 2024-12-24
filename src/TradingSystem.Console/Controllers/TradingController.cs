using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using TradingSystem.Common.Interfaces;
using TradingSystem.Common.Models;

namespace TradingSystem.Console.Controllers;

/// <summary>
/// Controller that exposes REST API endpoints for trading operations.
/// Uses a simulated exchange adapter for testing and development.
/// Provides endpoints for order management, market data, and account information.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TradingController : ControllerBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IExchangeAdapter _exchangeAdapter;
    private readonly ILogger<TradingController> _logger;

    public TradingController(
        IServiceScopeFactory scopeFactory,
        IExchangeAdapter exchangeAdapter,
        ILogger<TradingController> logger)
    {
        _scopeFactory = scopeFactory;
        _exchangeAdapter = exchangeAdapter;
        _logger = logger;
    }

    /// <summary>
    /// Get the current balance for a specified asset.
    /// </summary>
    /// <param name="asset">The asset symbol (default: USDT)</param>
    /// <returns>The current balance amount</returns>
    /// <remarks>
    /// For testing purposes, this returns a simulated balance of 10,000 USDT.
    /// In production, this would connect to the actual exchange account.
    /// </remarks>
    [HttpGet("balance")]
    public async Task<ActionResult<decimal>> GetBalance(string asset = "USDT")
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var balance = await _exchangeAdapter.GetAccountBalance(asset);
            return Ok(balance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting balance for {Asset}", asset);
            return StatusCode(500, "Error getting balance");
        }
    }

    /// <summary>
    /// Get current market data for a specified symbol.
    /// </summary>
    /// <param name="symbol">The trading pair symbol (e.g., BTCUSD)</param>
    /// <returns>Current market data including OHLCV</returns>
    /// <remarks>
    /// Returns simulated market data with realistic price movements.
    /// Includes order book and recent trades for market depth analysis.
    /// </remarks>
    [HttpGet("market-data/{symbol}")]
    public async Task<ActionResult<MarketData>> GetMarketData(string symbol)
    {
        try
        {
            var data = await _exchangeAdapter.GetMarketData(symbol);
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting market data for {Symbol}", symbol);
            return StatusCode(500, "Error getting market data");
        }
    }

    /// <summary>
    /// Place a new trading order.
    /// </summary>
    /// <param name="request">Order details including symbol, quantity, price, and direction</param>
    /// <returns>The created trade object</returns>
    /// <remarks>
    /// Supports market and limit orders.
    /// Automatically sets stop loss and take profit levels.
    /// Validates order parameters before execution.
    /// </remarks>
    [HttpPost("place-order")]
    public async Task<ActionResult<Trade>> PlaceOrder([FromBody] OrderRequest request)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var tradingService = scope.ServiceProvider.GetRequiredService<ITradingService>();
            var trade = await _exchangeAdapter.PlaceOrder(
                request.Symbol,
                request.Quantity,
                request.Price,
                request.IsLong,
                request.OrderType);

            return Ok(trade);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error placing order");
            return StatusCode(500, "Error placing order");
        }
    }

    /// <summary>
    /// Close an existing open order.
    /// </summary>
    /// <param name="orderId">The ID of the order to close</param>
    /// <returns>The updated trade object with closing details</returns>
    /// <remarks>
    /// Calculates realized PnL on closure.
    /// Updates trade status and timestamps.
    /// Removes order from open orders list.
    /// </remarks>
    [HttpPost("close-order/{orderId}")]
    public async Task<ActionResult<Trade>> CloseOrder(string orderId)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var tradingService = scope.ServiceProvider.GetRequiredService<ITradingService>();
            var trade = await _exchangeAdapter.CloseOrder(orderId);
            return Ok(trade);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing order {OrderId}", orderId);
            return StatusCode(500, "Error closing order");
        }
    }

    /// <summary>
    /// Get all currently open orders.
    /// </summary>
    /// <returns>List of open trades</returns>
    /// <remarks>
    /// Returns all orders with Open status.
    /// Includes full trade details for each order.
    /// Orders are tracked by the simulated exchange adapter.
    /// </remarks>
    [HttpGet("open-orders")]
    public ActionResult<List<Trade>> GetOpenOrders()
    {
        try
        {
            var orders = _exchangeAdapter.GetOpenOrders();
            return Ok(new { orders });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting open orders");
            return StatusCode(500, "Error getting open orders");
        }
    }
}

/// <summary>
/// Model for order placement requests.
/// Contains all necessary parameters for creating a new order.
/// </summary>
public class OrderRequest
{
    public string Symbol { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public bool IsLong { get; set; }
    public OrderType OrderType { get; set; }
}
