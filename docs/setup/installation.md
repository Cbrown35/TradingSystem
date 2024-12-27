# Installation Guide

## Prerequisites

### System Requirements
- CPU: 4+ cores recommended
- RAM: 16GB minimum, 32GB recommended
- Storage: 100GB+ SSD recommended
- Network: Stable internet connection with low latency

### Required Software
- .NET Core SDK 3.1 or later
- Docker Desktop
- Git
- PostgreSQL 13+ with TimescaleDB extension
- Node.js 14+ (for frontend tools)

## Installation Steps

### 1. Clone the Repository
```bash
git clone https://github.com/yourusername/tradingsystem.git
cd tradingsystem
```

### 2. Environment Setup

#### Configure Environment Variables
Create a `.env` file in the root directory:
```env
ASPNETCORE_ENVIRONMENT=Development
DB_CONNECTION_STRING=Host=localhost;Database=tradingdb;Username=trading;Password=your_password
EXCHANGE_API_KEY=your_api_key
EXCHANGE_API_SECRET=your_api_secret
```

#### Database Setup
```bash
# Create the database
psql -U postgres -c "CREATE DATABASE tradingdb;"

# Install TimescaleDB extension
psql -U postgres -d tradingdb -c "CREATE EXTENSION IF NOT EXISTS timescaledb CASCADE;"

# Run migrations
dotnet ef database update
```

### 3. Build and Run with Docker

#### Using Docker Compose
```bash
# Build the images
docker-compose build

# Start the services
docker-compose up -d
```

#### Manual Docker Build
```bash
# Build the trading system image
docker build -t tradingsystem .

# Run the container
docker run -d -p 5000:80 tradingsystem
```

### 4. Monitoring Setup

#### Prometheus
```bash
# Start Prometheus
docker-compose -f docker-compose.monitoring.yml up -d prometheus
```

#### Grafana
```bash
# Start Grafana
docker-compose -f docker-compose.monitoring.yml up -d grafana

# Access Grafana dashboard at http://localhost:3000
# Default credentials: admin/admin
```

## Configuration

### API Configuration
Edit `appsettings.json`:
```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5000",
    "Version": "v1",
    "RateLimit": 100
  }
}
```

### Trading Parameters
Edit `trading-config.json`:
```json
{
  "TradingSettings": {
    "DefaultLeverage": 1,
    "MaxPositionSize": 100000,
    "RiskPercentage": 1
  }
}
```

## Verification

### Health Check
```bash
curl http://localhost:5000/health
```

### API Test
```bash
curl http://localhost:5000/api/v1/market-data/BTC-USD/candles
```

### Log Verification
```bash
docker-compose logs -f tradingsystem
```

## Troubleshooting

### Common Issues

#### Database Connection
```bash
# Check database status
docker-compose ps postgres

# View database logs
docker-compose logs postgres
```

#### API Issues
```bash
# Check API status
curl http://localhost:5000/health

# View API logs
docker-compose logs api
```

#### Permission Issues
```bash
# Fix file permissions
chmod +x scripts/*.sh

# Fix Docker socket permissions
sudo chmod 666 /var/run/docker.sock
```

## Security Considerations

### Firewall Configuration
```bash
# Allow trading system ports
sudo ufw allow 5000/tcp  # API
sudo ufw allow 5432/tcp  # PostgreSQL
sudo ufw allow 9090/tcp  # Prometheus
sudo ufw allow 3000/tcp  # Grafana
```

### SSL Setup
1. Generate SSL certificate
2. Configure Nginx reverse proxy
3. Update API endpoints to use HTTPS

## Next Steps

1. Configure exchange API keys
2. Set up monitoring alerts
3. Configure trading strategies
4. Run backtests
5. Start with paper trading

## Support

- GitHub Issues: [Project Issues](https://github.com/yourusername/tradingsystem/issues)
- Documentation: [Project Wiki](https://github.com/yourusername/tradingsystem/wiki)
- Community: [Discord Channel](https://discord.gg/tradingsystem)
