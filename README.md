# Algorithmic Trading System {BETA}

A comprehensive algorithmic trading system built with .NET 8, designed for developing, testing, and executing trading strategies across multiple markets.

## Architecture

The system is organized into several key layers:

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

### Data Management
- Historical data storage
- Real-time data processing
- Market statistics calculation
- Data normalization

### Risk Management
- Position sizing
- Stop loss management
- Portfolio allocation
- Exposure monitoring

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
