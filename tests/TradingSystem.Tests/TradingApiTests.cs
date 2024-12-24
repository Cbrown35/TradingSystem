using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using TradingSystem.Common.Models;
using Xunit;

namespace TradingSystem.Tests;

/// <summary>
/// Integration tests for the Trading API endpoints. These tests verify the complete trading workflow
/// including order placement, retrieval, and closure using the simulated exchange adapter.
/// </summary>
[Collection("Trading API Tests")]
public class TradingApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TradingApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    [Trait("Category", "TradingApi")]
    /// <summary>
    /// Tests that a valid order can be placed successfully.
    /// Verifies:
    /// 1. Order creation with correct properties
    /// 2. Response status code is successful
    /// 3. Order details match the request
    /// 4. Order is in Open status
    /// </summary>
    public async Task PlaceOrder_ValidOrder_ReturnsCreatedOrder()
    {
        // Arrange
        var order = new OrderRequest
        {
            Symbol = "BTCUSD",
            Quantity = 0.1m,
            Price = 50000m,
            IsLong = true,
            OrderType = OrderType.Market
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Trading/place-order", order);
        var trade = await response.Content.ReadFromJsonAsync<Trade>();

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.NotNull(trade);
        Assert.Equal(order.Symbol, trade.Symbol);
        Assert.Equal(order.Quantity, trade.Quantity);
        Assert.Equal(order.Price, trade.EntryPrice);
        Assert.Equal(TradeStatus.Open, trade.Status);
    }

    [Fact]
    [Trait("Category", "TradingApi")]
    /// <summary>
    /// Tests that open orders can be retrieved successfully.
    /// Verifies:
    /// 1. Orders are tracked correctly
    /// 2. Response includes placed orders
    /// 3. Order list is not empty after placing an order
    /// </summary>
    public async Task GetOpenOrders_HasOrders_ReturnsOrderList()
    {
        // Arrange
        var order = new OrderRequest
        {
            Symbol = "BTCUSD",
            Quantity = 0.1m,
            Price = 50000m,
            IsLong = true,
            OrderType = OrderType.Market
        };
        await _client.PostAsJsonAsync("/api/Trading/place-order", order);

        // Act
        var response = await _client.GetAsync("/api/Trading/open-orders");
        var result = await response.Content.ReadFromJsonAsync<OpenOrdersResponse>();

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.NotNull(result);
        Assert.NotEmpty(result.Orders);
    }

    [Fact]
    [Trait("Category", "TradingApi")]
    /// <summary>
    /// Tests that an order can be closed successfully.
    /// Verifies:
    /// 1. Order status changes to Closed
    /// 2. Close time is recorded
    /// 3. PnL is calculated
    /// 4. Original order details are preserved
    /// </summary>
    public async Task CloseOrder_ValidOrder_ReturnsClosedOrder()
    {
        // Arrange
        var order = new OrderRequest
        {
            Symbol = "BTCUSD",
            Quantity = 0.1m,
            Price = 50000m,
            IsLong = true,
            OrderType = OrderType.Market
        };
        var placeResponse = await _client.PostAsJsonAsync("/api/Trading/place-order", order);
        var trade = await placeResponse.Content.ReadFromJsonAsync<Trade>();

        // Act
        var response = await _client.PostAsync($"/api/Trading/close-order/{trade.Id}", null);
        var closedTrade = await response.Content.ReadFromJsonAsync<Trade>();

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.NotNull(closedTrade);
        Assert.Equal(trade.Id, closedTrade.Id);
        Assert.Equal(TradeStatus.Closed, closedTrade.Status);
        Assert.NotNull(closedTrade.CloseTime);
        Assert.NotNull(closedTrade.RealizedPnL);
    }

    [Fact]
    [Trait("Category", "TradingApi")]
    /// <summary>
    /// Tests that market data can be retrieved for a valid symbol.
    /// Verifies:
    /// 1. All OHLCV fields are populated
    /// 2. Symbol matches request
    /// 3. Price values are non-zero
    /// 4. Volume data is present
    /// </summary>
    public async Task GetMarketData_ValidSymbol_ReturnsMarketData()
    {
        // Act
        var response = await _client.GetAsync("/api/Trading/market-data/BTCUSD");
        var marketData = await response.Content.ReadFromJsonAsync<MarketData>();

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.NotNull(marketData);
        Assert.Equal("BTCUSD", marketData.Symbol);
        Assert.NotEqual(0, marketData.Open);
        Assert.NotEqual(0, marketData.High);
        Assert.NotEqual(0, marketData.Low);
        Assert.NotEqual(0, marketData.Close);
        Assert.NotEqual(0, marketData.Volume);
    }

    [Fact]
    [Trait("Category", "TradingApi")]
    /// <summary>
    /// Tests that account balance can be retrieved.
    /// Verifies:
    /// 1. Initial balance is correct (10,000 USDT)
    /// 2. Response is successful
    /// </summary>
    public async Task GetBalance_ReturnsBalance()
    {
        // Act
        var response = await _client.GetAsync("/api/Trading/balance?asset=USDT");
        var balance = await response.Content.ReadFromJsonAsync<decimal>();

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(10000m, balance);
    }

    [Fact]
    [Trait("Category", "TradingApi")]
    /// <summary>
    /// Tests error handling for invalid symbol in order placement.
    /// Verifies:
    /// 1. Invalid symbols are rejected
    /// 2. Appropriate error status code is returned
    /// 3. System handles invalid input gracefully
    /// </summary>
    public async Task PlaceOrder_InvalidSymbol_ReturnsBadRequest()
    {
        // Arrange
        var order = new OrderRequest
        {
            Symbol = "INVALID",
            Quantity = 0.1m,
            Price = 50000m,
            IsLong = true,
            OrderType = OrderType.Market
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Trading/place-order", order);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "TradingApi")]
    /// <summary>
    /// Tests error handling for invalid order ID in close operation.
    /// Verifies:
    /// 1. Invalid order IDs are rejected
    /// 2. Appropriate error status code is returned
    /// 3. System handles invalid input gracefully
    /// </summary>
    public async Task CloseOrder_InvalidOrderId_ReturnsBadRequest()
    {
        // Act
        var response = await _client.PostAsync("/api/Trading/close-order/invalid-id", null);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }
}

/// <summary>
/// Response model for the open orders endpoint.
/// Wraps the list of trades in a dedicated response object for better API design.
/// </summary>
public class OpenOrdersResponse
{
    public List<Trade> Orders { get; set; } = new();
}
