# Trading System Health Check UI

A comprehensive health monitoring system for the algorithmic trading platform, providing real-time health status, metrics, and notifications.

## Features

### 1. Health Status Monitoring
- Real-time monitoring of system components
- Customizable health checks for critical services
- Status history and trend analysis
- Detailed component status views

### 2. Authentication & Authorization
- Secure access control
- Role-based permissions
- Multiple authentication providers:
  - Basic authentication
  - LDAP integration
  - OAuth support
  - API key authentication

### 3. Performance Optimization
- Response caching
- Compression support (Gzip, Deflate, Brotli)
- Rate limiting
- Request logging and analytics

### 4. Notifications
- Multi-channel alerts:
  - Email notifications
  - Slack integration
  - PagerDuty integration
  - Custom webhooks
- Configurable notification rules
- Alert throttling and aggregation

### 5. Metrics & Analytics
- Performance metrics
- System resource monitoring
- Trading metrics integration
- Custom metric support

## Configuration

### Basic Setup

```json
{
  "HealthChecks": {
    "Enabled": true,
    "IntervalSeconds": 30,
    "UI": {
      "Path": "/healthchecks-ui",
      "ApiPath": "/healthchecks-api",
      "Authentication": {
        "Enabled": true,
        "Provider": "basic"
      }
    }
  }
}
```

### Authentication Configuration

```json
{
  "HealthChecks": {
    "UI": {
      "Authentication": {
        "Provider": "basic",
        "BasicAuth": {
          "Username": "admin",
          "Password": "secure_password"
        },
        "LdapAuth": {
          "Server": "ldap://directory.example.com",
          "Domain": "example.com",
          "UserDnFormat": "CN={0},OU=Users,DC=example,DC=com"
        },
        "OAuthConfig": {
          "TokenEndpoint": "https://auth.example.com/token",
          "ClientId": "health_checks",
          "ClientSecret": "client_secret",
          "Scope": "health_check_access"
        }
      }
    }
  }
}
```

### Performance Configuration

```json
{
  "HealthChecks": {
    "Cache": {
      "Enabled": true,
      "TimeToLiveSeconds": 30,
      "SlidingExpirationSeconds": 10
    },
    "Compression": {
      "Enabled": true,
      "Level": "optimal",
      "MinimumSizeBytes": 1024,
      "PreferBrotli": true
    },
    "RateLimit": {
      "Enabled": true,
      "RequestsPerMinute": 60,
      "BurstSize": 10
    }
  }
}
```

### Notification Configuration

```json
{
  "HealthChecks": {
    "Notifications": {
      "Enabled": true,
      "MinimumSecondsBetweenNotifications": 300,
      "Channels": [
        {
          "Name": "critical_alerts",
          "Type": "email",
          "Settings": {
            "To": "alerts@example.com",
            "From": "health@example.com",
            "SmtpServer": "smtp.example.com"
          }
        },
        {
          "Name": "team_notifications",
          "Type": "slack",
          "Settings": {
            "WebhookUrl": "https://hooks.slack.com/services/..."
          }
        }
      ]
    }
  }
}
```

## Usage

### Adding Health Checks

```csharp
services.AddTradingSystemHealthChecks(config =>
{
    // Core trading system checks
    config.AddCheck<TradingSystemHealthCheck>("trading_system");
    
    // Database health
    config.AddDbContextCheck<TradingContext>("database");
    
    // Market data feed
    config.AddCheck<MarketDataHealthCheck>("market_data");
    
    // Risk management
    config.AddCheck<RiskManagementHealthCheck>("risk_management");
});
```

### Configuring Middleware

```csharp
app.UseHealthCheckPipeline();
```

### Securing Endpoints

```csharp
[HealthCheckAuth(HealthCheckPolicies.ViewHealthChecks)]
public class HealthCheckController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetHealthStatus()
    {
        // Implementation
    }
}
```

### Custom Health Checks

```csharp
public class TradingSystemHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Perform health check logic
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(ex.Message);
        }
    }
}
```

## API Endpoints

### Health Status
- `GET /health` - Overall system health status
- `GET /health/ready` - Readiness check
- `GET /health/live` - Liveness check

### UI Endpoints
- `GET /healthchecks-ui` - Health check dashboard
- `GET /healthchecks-api/healthchecks` - Health checks data
- `GET /healthchecks-api/healthchecks/history` - Historical data

### Analytics
- `GET /health/analytics` - Health check analytics
- `GET /health/issues` - Current issues
- `GET /metrics` - System metrics

## Development

### Prerequisites
- .NET 8 SDK
- Redis (for distributed caching)
- PostgreSQL (for storage)

### Building
```bash
dotnet build
```

### Testing
```bash
dotnet test
```

### Running Locally
```bash
dotnet run
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.
