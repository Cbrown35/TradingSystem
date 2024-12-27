# Configuration Guide

## Environment Configuration

### Environment Variables
```env
# Application Settings
ASPNETCORE_ENVIRONMENT=Development|Staging|Production
APP_NAME=TradingSystem
APP_VERSION=1.0.0

# Database Configuration
DB_HOST=localhost
DB_PORT=5432
DB_NAME=tradingdb
DB_USER=trading
DB_PASSWORD=your_secure_password
DB_SSL_MODE=require

# Exchange API Credentials
EXCHANGE_API_KEY=your_api_key
EXCHANGE_API_SECRET=your_api_secret
EXCHANGE_PASSPHRASE=optional_passphrase

# Monitoring Configuration
PROMETHEUS_ENDPOINT=http://localhost:9090
GRAFANA_ENDPOINT=http://localhost:3000
```

## Application Settings

### Core Configuration (appsettings.json)
```json
{
  "ApplicationSettings": {
    "Name": "Trading System",
    "Environment": "Development",
    "Version": "1.0.0",
    "ApiVersion": "v1",
    "EnableSwagger": true
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "System": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### Database Configuration
```json
{
  "DatabaseSettings": {
    "Provider": "PostgreSQL",
    "EnableRetry": true,
    "MaxRetryAttempts": 3,
    "CommandTimeout": 30,
    "EnableDetailedErrors": true,
    "EnableSensitiveDataLogging": false
  },
  "TimescaleDB": {
    "ChunkTimeInterval": "7 days",
    "RetentionPolicy": "90 days",
    "CompressionEnabled": true
  }
}
```

## Trading Configuration

### Risk Management (risk-config.json)
```json
{
  "RiskManagement": {
    "MaxDrawdown": 0.02,
    "MaxPositionSize": 100000,
    "MaxLeverage": 3,
    "StopLossPercentage": 0.01,
    "DailyLossLimit": 0.05
  }
}
```

### Strategy Configuration (strategy-config.json)
```json
{
  "Strategies": {
    "MovingAverageCrossover": {
      "Enabled": true,
      "Parameters": {
        "ShortPeriod": 10,
        "LongPeriod": 20,
        "SignalThreshold": 0.001
      }
    },
    "RSIStrategy": {
      "Enabled": true,
      "Parameters": {
        "Period": 14,
        "OverboughtThreshold": 70,
        "OversoldThreshold": 30
      }
    }
  }
}
```

## Monitoring Configuration

### Prometheus (prometheus.yml)
```yaml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

scrape_configs:
  - job_name: 'trading_system'
    static_configs:
      - targets: ['localhost:5000']
```

### Grafana (grafana.ini)
```ini
[server]
http_port = 3000
domain = localhost

[security]
admin_user = admin
admin_password = admin

[auth.anonymous]
enabled = false
```

## Logging Configuration

### Serilog Settings (serilog.json)
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/trading-.log",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

## Security Configuration

### JWT Settings (jwt-config.json)
```json
{
  "JwtSettings": {
    "SecretKey": "your-secret-key-min-16-chars",
    "Issuer": "TradingSystem",
    "Audience": "TradingSystemAPI",
    "ExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

### CORS Configuration
```json
{
  "CorsSettings": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "https://yourdomain.com"
    ],
    "AllowedMethods": ["GET", "POST", "PUT", "DELETE"],
    "AllowedHeaders": ["Authorization", "Content-Type"]
  }
}
```

## Exchange Integration

### Exchange Settings (exchange-config.json)
```json
{
  "ExchangeSettings": {
    "Name": "Binance",
    "BaseUrl": "https://api.binance.com",
    "WebsocketUrl": "wss://stream.binance.com:9443",
    "RateLimit": {
      "MaxRequestsPerMinute": 1200,
      "MaxOrdersPerSecond": 10
    }
  }
}
```

## Performance Tuning

### Memory Management
```json
{
  "MemorySettings": {
    "GCSettings": {
      "LargeObjectHeapCompactionMode": "CompactOnce",
      "LatencyMode": "SustainedLowLatency"
    },
    "CacheSettings": {
      "DefaultExpirationMinutes": 5,
      "MaxCacheSize": "1GB"
    }
  }
}
```

### Thread Pool Settings
```json
{
  "ThreadPoolSettings": {
    "MinWorkerThreads": 4,
    "MinIoThreads": 4,
    "MaxWorkerThreads": 100,
    "MaxIoThreads": 100
  }
}
```

## Deployment Configuration

### Docker Settings (docker-compose.yml)
```yaml
version: '3.8'
services:
  tradingsystem:
    build:
      context: .
      dockerfile: Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    ports:
      - "5000:80"
    depends_on:
      - postgres
      - redis
```

## Backup Configuration

### Backup Settings (backup-config.json)
```json
{
  "BackupSettings": {
    "AutomaticBackup": true,
    "BackupInterval": "24h",
    "RetentionDays": 30,
    "BackupPath": "/backup",
    "CompressionEnabled": true
  }
}
