# Infrastructure Layer

The Infrastructure layer implements the persistence and external service concerns of the Trading System. It contains concrete implementations of repository interfaces defined in the Common layer, along with database context, configurations, and data access patterns.

## Components

### Repositories

- **TradeRepository**: Manages trade data persistence and retrieval
- **OrderRepository**: Handles order data storage and querying
- **MarketDataRepository**: Manages market data storage and analysis

### Database

- **TradingContext**: Entity Framework Core DbContext for the trading system
- **DatabaseConfig**: Configuration settings for database connection and behavior
- **DatabaseFactory**: Factory methods for database initialization and management

### Dependency Injection

The `DependencyInjection.cs` file provides extension methods to configure Infrastructure services:

```csharp
services.AddInfrastructure(databaseConfig);
```

## Setup

1. Install required packages:
```bash
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

2. Configure database connection:
```json
{
  "Database": {
    "ConnectionString": "Host=localhost;Database=tradingsystem;Username=postgres;Password=postgres",
    "EnableSensitiveDataLogging": false,
    "EnableDetailedErrors": false,
    "CommandTimeout": 30,
    "MaxRetryCount": 3,
    "EnableAutoMigration": true
  }
}
```

3. Initialize database:
```csharp
await serviceProvider.InitializeInfrastructureAsync(databaseConfig);
```

## Features

### Repository Pattern
- Generic repository base class with common CRUD operations
- Specialized repositories for different entity types
- Async/await pattern throughout
- Bulk operations support

### Database Operations
- Automatic migrations
- Health checks
- Backup and restore
- Connection resilience
- Performance monitoring

### Market Data Management
- Historical data storage
- Real-time data processing
- Market statistics calculation
- Liquidity analysis
- Volume profile analysis
- Correlation analysis

### Trade & Order Management
- Trade lifecycle tracking
- Order status management
- Performance metrics calculation
- Risk metrics tracking

## Usage Examples

### Adding a Trade
```csharp
var tradeRepo = serviceProvider.GetRequiredService<ITradeRepository>();
var trade = new Trade { /* ... */ };
await tradeRepo.AddTrade(trade);
```

### Querying Market Data
```csharp
var marketDataRepo = serviceProvider.GetRequiredService<IMarketDataRepository>();
var historicalData = await marketDataRepo.GetHistoricalData(
    symbol: "BTCUSDT",
    startDate: DateTime.UtcNow.AddDays(-7),
    endDate: DateTime.UtcNow,
    timeFrame: TimeFrame.OneHour
);
```

### Managing Orders
```csharp
var orderRepo = serviceProvider.GetRequiredService<IOrderRepository>();
var openOrders = await orderRepo.GetOpenOrders("BTCUSDT");
```

## Health Checks

The infrastructure layer includes built-in health checks:
```csharp
services.AddInfrastructureHealthChecks();
```

Monitor database health:
```bash
curl http://localhost:5000/health
```

## Performance Considerations

- Connection pooling is enabled by default
- Bulk operations for large datasets
- Efficient query patterns with proper indexing
- Caching strategies for frequently accessed data
- Monitoring and diagnostics support

## Error Handling

- Retry policies for transient failures
- Detailed error logging
- Custom exceptions for specific scenarios
- Validation before persistence

## Security

- Connection string encryption
- Sensitive data logging control
- User-based access control
- Audit logging support

## Maintenance

### Backup
```csharp
await serviceProvider.BackupInfrastructureAsync("backup.sql");
```

### Restore
```csharp
await serviceProvider.RestoreInfrastructureAsync("backup.sql");
```

### Reset
```csharp
await serviceProvider.ResetInfrastructureAsync();
```

## Contributing

1. Follow the repository pattern
2. Add appropriate unit tests
3. Update documentation
4. Use async/await consistently
5. Handle errors appropriately
6. Add proper logging

## Dependencies

- .NET 8.0
- EntityFrameworkCore 8.0.0
- Npgsql.EntityFrameworkCore.PostgreSQL 8.0.0
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging
