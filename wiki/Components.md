# System Components

## Common Module

The Common module provides core interfaces and models used throughout the system.

### Key Interfaces
- `IExchangeAdapter`: Exchange connectivity
- `IMarketDataService`: Market data operations
- `IRiskManager`: Risk management
- `ITradingStrategy`: Strategy implementation
- `ITradeRepository`: Trade data access

### Models
- `Trade`: Trade execution details
- `Order`: Order management
- `MarketData`: Market information
- `Signal`: Trading signals
- `Theory`: Trading theories

## Core Module

Central system coordination and monitoring.

### Services
- `TradingSystemCoordinator`: System orchestration
- `HealthCheckService`: System health monitoring
- `MonitoringService`: Metrics and monitoring

### Monitoring
- Health checks implementation
- Metrics collection
- Alert configuration
- Dashboard management

## Infrastructure Module

Data access and persistence layer.

### Components
- `TradingContext`: Entity Framework context
- `DatabaseFactory`: Database connection management
- Repository implementations:
  - `MarketDataRepository`
  - `OrderRepository`
  - `TradeRepository`

## RealTrading Module

Live trading implementation and exchange integration.

### Services
- `TradingService`: Trade execution
- `RiskManager`: Risk assessment
- `MarketDataService`: Market data processing
- Exchange Adapters:
  - `TradingViewAdapter`
  - `TradovateAdapter`
  - `SimulatedExchangeAdapter`

### Configuration
- `RiskManagerConfig`: Risk parameters
- `MarketDataCacheConfig`: Cache settings
- `ExchangeConfig`: Exchange settings

## StrategySearch Module

Trading strategy development and optimization.

### Components
- `StrategyOptimizer`: Strategy optimization
- `Backtester`: Strategy testing
- `TheoryGenerator`: Trading theory development

### Strategies
- `SimpleMovingAverageStrategy`
- `TheoryBasedStrategy`

## Console Application

Command-line interface for system interaction.

### Features
- System initialization
- Command processing
- Configuration management
- Service factory implementation

## Monitoring Integration

### Grafana
- Trading dashboard
- Performance metrics
- Risk monitoring
- System health

### Prometheus
- Metric collection
- Alert rules
- Recording rules
- Notification templates

## Docker Support

### Containers
- Application services
- Monitoring stack
- Database
- Message queue

### Configuration
- Development compose
- Production compose
- Environment overrides
