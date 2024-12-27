# Trading System Implementation

This directory contains the trading system implementation, including exchange adapters, market data handling, and order management.

## Components

### SimulatedExchangeAdapter

A test implementation of `IExchangeAdapter` that provides:
- Simulated market data
- Order execution
- PnL calculation
- Balance tracking

Key features:
- Realistic price movements
- Order book simulation
- Historical data generation
- Trade lifecycle management

### Trading API Endpoints

The system exposes REST endpoints for trading operations:

```http
POST /api/Trading/place-order
{
    "symbol": "BTCUSD",
    "quantity": 0.1,
    "price": 50000,
    "isLong": true,
    "orderType": 0
}

GET /api/Trading/open-orders
GET /api/Trading/market-data/{symbol}
POST /api/Trading/close-order/{orderId}
GET /api/Trading/balance?asset=USDT
```

## Testing

### Unit Tests

Run trading-specific tests:
```bash
dotnet test --filter Category=Trading
```

### Integration Tests

Run API integration tests:
```bash
dotnet test --filter Category=TradingApi
```

### Manual Testing

1. Start the system:
```bash
dotnet run --project src/TradingSystem.Console/TradingSystem.Console.csproj
```

2. Use Swagger UI:
- Navigate to `http://localhost:3000/swagger`
- Test endpoints interactively

3. Use cURL:
```bash
# Place order
curl -X POST http://localhost:3000/api/Trading/place-order \
  -H "Content-Type: application/json" \
  -d '{
    "symbol": "BTCUSD",
    "quantity": 0.1,
    "price": 50000,
    "isLong": true,
    "orderType": 0
  }'

# View open orders
curl http://localhost:3000/api/Trading/open-orders

# Close order
curl -X POST http://localhost:3000/api/Trading/close-order/{orderId}
```

## Implementation Details

### Market Data

The simulated exchange provides:
- OHLCV data
- Order book depth (10 levels)
- Recent trades
- Volume profiles

### Order Types

Supported order types:
- Market orders
- Limit orders (planned)
- Stop orders (planned)
- Take profit orders (planned)

### Risk Management

Risk controls are configured according to the reference configuration (`docs/references/reference_config.json`):
- Market-specific volatility adjustments
  * Crypto: 1.5x standard
  * Futures: 1.3x standard
  * Equities: 1.2x standard
- Position sizing based on market parameters
- Dynamic stop loss and take profit levels
- Maximum drawdown limits per market type

See `docs/risk/parameters.md` for detailed risk management documentation.

### Trading Pairs

Supported symbols:
- BTCUSD
- ETHUSD
- XRPUSD

Each pair includes:
- Real-time price updates
- Historical data (30 days)
- Order book simulation
- Trade execution

## Development

### Adding New Features

1. Exchange Integration:
```csharp
public class MyExchangeAdapter : IExchangeAdapter
{
    // Implement exchange-specific methods
}
```

2. Register in DI:
```csharp
services.AddScoped<IExchangeAdapter, MyExchangeAdapter>();
```

### Testing New Features

1. Create test class:
```csharp
[Collection("Trading API Tests")]
public class MyTradingTests
{
    [Fact]
    [Trait("Category", "TradingApi")]
    public async Task MyTest()
    {
        // Test implementation
    }
}
```

2. Run specific tests:
```bash
dotnet test --filter TestCategory=TradingApi
```

## Configuration

The system uses a multi-layered configuration approach:

### Core Configuration
- `appsettings.json`: Basic application settings
- `docs/references/reference_config.json`: Trading parameters and market configurations
- `docs/setup/configuration.md`: Detailed configuration guide

### Market-Specific Settings
Configuration is tailored by market type (see `docs/references/reference_config.json`):
```json
{
  "crypto": {
    "volatility_adjustment": 1.5,
    "position_size_limit": 0.1,
    "min_order_size": {
      "BTC": 0.001,
      "ETH": 0.01
    }
  }
}
```

### Documentation
For detailed configuration information, see:
- `docs/setup/configuration.md`: Setup and configuration guide
- `docs/risk/parameters.md`: Risk management parameters
- `docs/references/trading_principles.md`: Core trading principles

## Monitoring

Trading metrics available:
- Order execution time
- Fill rates
- Slippage
- PnL tracking
- Position exposure

## Future Enhancements

Planned features:
- Advanced order types
- Portfolio management
- Risk analysis
- Performance reporting
- Machine learning integration
