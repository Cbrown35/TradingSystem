# Algorithmic Trading System {BETA}

A comprehensive algorithmic trading system built with .NET 8, designed for developing, testing, and executing trading strategies across multiple markets.

## Documentation

The system includes comprehensive documentation in the `docs/` directory:

### Reference Documentation (`docs/references/`)
- Trading principles and core concepts
- Market-specific parameters and configurations
- Book references and study materials
- Implementation guidelines

### API Documentation (`docs/api/`)
- Detailed endpoint specifications
- Request/response schemas
- Authentication requirements
- Rate limits

### Setup Guides (`docs/setup/`)
- Installation instructions
- Configuration guidelines
- Environment setup
- Deployment procedures

### Design Documentation (`docs/design/`)
- System architecture
- Component interactions
- Data flow diagrams
- Design decisions

### Trading Documentation
- Backtesting methodology (`docs/backtesting/`)
- Risk management parameters (`docs/risk/`)
- Trading principles (`docs/principles/`)
- Example configurations (`docs/examples/`)

## Architecture

The system is organized into several key layers, following the principles defined in our reference documentation:

### Common Layer (`src/Common/`)
- Core domain models and interfaces
- Shared utilities and extensions
- Cross-cutting concerns

### Core Layer (`src/Core/`)
- Core business logic
- Trading system coordination
- Strategy execution engine

### Infrastructure Layer (`src/Infrastructure/`)
- Data persistence
- External service integrations
- Repository implementations

### Strategy Search Layer (`src/StrategySearch/`)
- Strategy generation and optimization
- Backtesting engine
- Performance analysis

### Real Trading Layer (`src/RealTrading/`)
- Live trading execution
- Exchange integrations
- Risk management
- Market data processing

### Console Application (`src/TradingSystem.Console/`)
- Command-line interface
- System configuration
- Trading operations

## Features

### Strategy Development
- Flexible strategy definition framework
- Multiple timeframe support
- Custom indicator development
- Signal generation and filtering

### Strategy Search
- Genetic algorithm optimization
- Parameter space exploration
- Multi-objective optimization
- Performance metrics analysis

### Backtesting
- Historical data simulation
- Transaction cost modeling
- Risk metrics calculation
- Performance reporting

### Real-Time Trading
- Multiple exchange support
- Order management
- Position tracking
- Risk control
- Market data streaming

### API Documentation
- Swagger UI integration
- Interactive API testing
- Endpoint documentation
- Request/response schema documentation

### Trading API Endpoints

The system exposes the following REST API endpoints for trading:

#### Place Order
```http
POST /api/Trading/place-order
Content-Type: application/json

{
    "symbol": "BTCUSD",
    "quantity": 0.1,
    "price": 50000,
    "isLong": true,
    "orderType": 0
}
```

#### Get Open Orders
```http
GET /api/Trading/open-orders
```

#### Close Order
```http
POST /api/Trading/close-order/{orderId}
```

#### Get Market Data
```http
GET /api/Trading/market-data/{symbol}
```

#### Get Account Balance
```http
GET /api/Trading/balance?asset=USDT
```

### Testing Trading Functionality

1. Start the system:
```bash
dotnet run --project src/TradingSystem.Console/TradingSystem.Console.csproj
```

2. Place a test order:
```bash
curl -X POST http://localhost:3000/api/Trading/place-order \
  -H "Content-Type: application/json" \
  -d '{"symbol":"BTCUSD","quantity":0.1,"price":50000,"isLong":true,"orderType":0}'
```

3. View open orders:
```bash
curl http://localhost:3000/api/Trading/open-orders
```

4. Close an order (replace {orderId} with actual ID):
```bash
curl -X POST http://localhost:3000/api/Trading/close-order/{orderId}
```

5. Check market data:
```bash
curl http://localhost:3000/api/Trading/market-data/BTCUSD
```

The system includes a simulated exchange adapter for testing that:
- Maintains simulated prices for BTCUSD, ETHUSD, and XRPUSD
- Generates realistic order books and market data
- Simulates trade execution with PnL calculation
- Provides a test balance of 10,000 USDT

### Data Management
- Historical data storage
- Real-time data processing
- Market statistics calculation
- Data normalization

### Risk Management
- Position sizing (following reference configuration parameters)
- Stop loss management with market-specific adjustments
- Portfolio allocation based on risk metrics
- Exposure monitoring with configurable limits
- Market-specific volatility adjustments

## Getting Started

### Prerequisites
- .NET 8 SDK
- PostgreSQL 14+
- Docker (optional)

### Installation

1. Clone the repository:
```bash
git clone https://github.com/yourusername/tradingsystem.git
cd tradingsystem
```

2. Install dependencies:
```bash
dotnet restore
```

3. Set up the database:
```bash
cd src/Infrastructure
dotnet ef database update
```

4. Configure settings:

a. Set up application settings:
```bash
cp src/TradingSystem.Console/appsettings.json src/TradingSystem.Console/appsettings.Development.json
```

b. Set up secrets:
```bash
cp config/secrets.template.json config/secrets.json
```

c. Generate secure keys and passwords:
```bash
# On Unix/Linux/macOS
./scripts/generate-secrets.sh

# On Windows
.\scripts\generate-secrets.ps1
```

The secrets configuration includes:
- Database credentials
  - User: PostgreSQL username
  - Password: Generated secure password
  - Name: Database name
- Redis password for caching
- PgAdmin credentials
  - Email: Admin email
  - Password: Generated secure password
- Monitoring
  - Grafana admin password
- Security
  - JWT secret for authentication
  - API key for external integrations

d. Load the secrets into your environment:
```bash
# On Unix/Linux/macOS
source scripts/load-secrets.sh

# On Windows
.\scripts\load-secrets.ps1
```

5. Build the solution:
```bash
dotnet build
```

### Running the System

Start the console application:
```bash
cd src/TradingSystem.Console
dotnet run
```

Access the API documentation:
- Navigate to `http://localhost/swagger` in your browser when running in Development environment
- Interactive documentation of all API endpoints
- Test API endpoints directly from the Swagger UI

## Development

### Adding a New Strategy

1. Create a new strategy class:
```csharp
public class MyStrategy : ITradingStrategy
{
    public async Task<bool?> ShouldEnter(List<MarketData> data)
    {
        // Implement entry logic
    }

    public async Task<bool?> ShouldExit(List<MarketData> data)
    {
        // Implement exit logic
    }
}
```

2. Register the strategy:
```csharp
services.AddScoped<ITradingStrategy, MyStrategy>();
```

### Adding an Exchange Integration

1. Implement the IExchangeAdapter interface:
```csharp
public class MyExchangeAdapter : IExchangeAdapter
{
    // Implement exchange-specific methods
}
```

2. Register the adapter:
```csharp
services.AddScoped<IExchangeAdapter, MyExchangeAdapter>();
```

## Testing

Run unit tests:
```bash
dotnet test
```

Run integration tests:
```bash
dotnet test --filter Category=Integration
```

## Configuration

Key configuration files:
- `appsettings.json`: Application settings
- `trading-config.json`: Trading parameters
- `risk-config.json`: Risk management settings

## Deployment

1. Build for production:
```bash
dotnet publish -c Release
```

2. Deploy using Docker:
```bash
docker build -t tradingsystem .
docker run tradingsystem
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [Binance API](https://github.com/binance/binance-spot-api-docs)
- [TradingView](https://www.tradingview.com/)
- [TA-Lib](https://ta-lib.org/)

## Support

For support, email support@tradingsystem.com or join our Discord channel.

## Roadmap

- [ ] Machine learning integration
- [ ] Web interface
- [ ] Mobile app
- [ ] Additional exchange support
- [ ] Advanced portfolio management
- [ ] Real-time analytics dashboard
