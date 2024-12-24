# Testing Guide

This guide covers how to test the trading functionality of the system.

## Prerequisites

- System is running (`dotnet run --project src/TradingSystem.Console/TradingSystem.Console.csproj`)
- API is accessible at `http://localhost:3000`

## API Testing

### Using Swagger UI

1. Access Swagger UI:
   - Navigate to `http://localhost:3000/swagger` in your browser
   - All trading endpoints are under the Trading section

2. Test endpoints directly in Swagger UI:
   - Click on an endpoint to expand it
   - Click "Try it out"
   - Fill in the required parameters
   - Click "Execute"

### Using cURL

#### Place Order
```bash
curl -X POST http://localhost:3000/api/Trading/place-order \
  -H "Content-Type: application/json" \
  -d '{
    "symbol": "BTCUSD",
    "quantity": 0.1,
    "price": 50000,
    "isLong": true,
    "orderType": 0
  }'
```

Expected response:
```json
{
  "id": "387848c2-81a3-469b-b264-ce804f989020",
  "symbol": "BTCUSD",
  "direction": 0,
  "entryPrice": 50000,
  "quantity": 0.1,
  "status": 0,
  "openTime": "2024-12-24T01:31:43.927322Z"
}
```

#### View Open Orders
```bash
curl http://localhost:3000/api/Trading/open-orders
```

Expected response:
```json
{
  "orders": [
    {
      "id": "387848c2-81a3-469b-b264-ce804f989020",
      "symbol": "BTCUSD",
      "direction": 0,
      "entryPrice": 50000,
      "quantity": 0.1,
      "status": 0
    }
  ]
}
```

#### Close Order
```bash
curl -X POST http://localhost:3000/api/Trading/close-order/387848c2-81a3-469b-b264-ce804f989020
```

Expected response:
```json
{
  "id": "387848c2-81a3-469b-b264-ce804f989020",
  "status": 1,
  "closeTime": "2024-12-24T01:32:15.123456Z",
  "realizedPnL": 71
}
```

#### Get Market Data
```bash
curl http://localhost:3000/api/Trading/market-data/BTCUSD
```

Expected response:
```json
{
  "symbol": "BTCUSD",
  "timestamp": "2024-12-24T01:31:43.927322Z",
  "open": 50000,
  "high": 50050,
  "low": 49950,
  "close": 50025,
  "volume": 1000
}
```

#### Check Balance
```bash
curl http://localhost:3000/api/Trading/balance?asset=USDT
```

Expected response:
```json
10000.00
```

## Testing Workflow

1. Start with checking market data to see current prices
2. Place a test order with appropriate price and quantity
3. Verify the order appears in open orders
4. Close the order and check PnL
5. Verify order is removed from open orders

## Simulated Exchange Features

The system uses a simulated exchange adapter for testing that provides:

1. Market Data Simulation
   - Realistic price movements
   - Order book generation
   - Volume and trade data

2. Order Management
   - Order placement validation
   - Order status tracking
   - Order execution simulation

3. PnL Calculation
   - Entry and exit price tracking
   - Commission simulation
   - Slippage simulation

4. Test Balance
   - Starting balance: 10,000 USDT
   - Balance updates based on trades
   - Margin requirement simulation

## Common Test Scenarios

1. Basic Order Flow
   - Place market order
   - Check order status
   - Close order
   - Verify PnL

2. Multiple Orders
   - Place multiple orders
   - View all open orders
   - Close specific orders
   - Close all orders

3. Error Cases
   - Invalid symbol
   - Insufficient balance
   - Invalid order parameters
   - Invalid order ID

## Automated Testing

The system includes automated tests for trading functionality:

1. Unit Tests
```bash
dotnet test --filter Category=Trading
```

2. Integration Tests
```bash
dotnet test --filter Category=TradingIntegration
```

3. API Tests
```bash
dotnet test --filter Category=TradingApi
