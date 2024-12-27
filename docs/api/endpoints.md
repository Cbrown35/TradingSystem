# Trading System API Endpoints

## Authentication
All API endpoints require authentication using JWT tokens. Include the token in the Authorization header:
```
Authorization: Bearer <your_token>
```

## Trading Endpoints

### GET /api/v1/trading/positions
Get current trading positions.

**Response:**
```json
{
  "positions": [
    {
      "symbol": "BTC-USD",
      "size": "0.5",
      "entryPrice": "45000.00",
      "currentPrice": "46000.00",
      "unrealizedPnL": "500.00",
      "side": "LONG"
    }
  ]
}
```

### POST /api/v1/trading/orders
Place a new order.

**Request:**
```json
{
  "symbol": "BTC-USD",
  "side": "BUY",
  "type": "LIMIT",
  "quantity": "0.1",
  "price": "45000.00",
  "timeInForce": "GTC"
}
```

## Market Data Endpoints

### GET /api/v1/market-data/{symbol}/candles
Get historical candlestick data.

**Parameters:**
- interval: 1m, 5m, 15m, 1h, 4h, 1d
- limit: Number of candles (default: 100, max: 1000)
- startTime: ISO timestamp
- endTime: ISO timestamp

### GET /api/v1/market-data/{symbol}/orderbook
Get current orderbook snapshot.

## Risk Management Endpoints

### GET /api/v1/risk/metrics
Get current risk metrics.

### POST /api/v1/risk/parameters
Update risk parameters.

## Strategy Endpoints

### GET /api/v1/strategies
List available trading strategies.

### POST /api/v1/strategies/{id}/activate
Activate a trading strategy.

## Backtesting Endpoints

### POST /api/v1/backtest
Run a backtest simulation.

**Request:**
```json
{
  "strategy": "MovingAverageCrossover",
  "symbol": "BTC-USD",
  "startTime": "2024-01-01T00:00:00Z",
  "endTime": "2024-01-31T23:59:59Z",
  "parameters": {
    "shortPeriod": 10,
    "longPeriod": 20
  }
}
```

## Health Check Endpoints

### GET /health
System health check endpoint.

### GET /metrics
Prometheus metrics endpoint.

## Rate Limits
- 100 requests per minute for trading endpoints
- 1000 requests per minute for market data endpoints
- Backtest endpoints are limited to 10 requests per hour

## Error Responses
All endpoints follow a standard error response format:
```json
{
  "error": {
    "code": "INVALID_PARAMETERS",
    "message": "Invalid order quantity",
    "details": {
      "field": "quantity",
      "constraint": "must be greater than 0"
    }
  }
}
