# Setup Guide

This guide walks through the process of setting up the Trading System for development and production use.

## Prerequisites

- .NET 6.0 SDK or later
- Docker and Docker Compose
- Git
- PostgreSQL (for development)
- Visual Studio 2022 or VS Code with C# extensions

## Development Setup

1. **Clone the Repository**
   ```bash
   git clone <repository-url>
   cd tradingsystem
   ```

2. **Database Setup**
   ```bash
   # Initialize development database
   cd scripts
   ./init-dev-db.sql
   ```

3. **Configuration**
   - Copy `appsettings.json.example` to `appsettings.json`
   - Update PostgreSQL connection strings
   - Configure exchange API credentials

4. **Build the Solution**
   ```bash
   dotnet restore
   dotnet build
   ```

5. **Run Tests**
   ```bash
   dotnet test
   ```

## Docker Deployment

1. **Build Docker Images**
   ```bash
   docker-compose build
   ```

2. **Start Services**
   ```bash
   # Development
   docker-compose up -d
   
   # Production
   docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
   ```

3. **Verify Deployment**
   - Access monitoring: http://localhost:3000 (Grafana)
   - Check health endpoints: http://localhost:8080/health

## Monitoring Setup

1. **Grafana**
   - Default credentials in `grafana/grafana.ini`
   - Pre-configured dashboards in `grafana/provisioning/dashboards`
   - Data sources in `grafana/provisioning/datasources`

2. **Prometheus**
   - Configuration in `prometheus/prometheus.yml`
   - Alert rules in `prometheus/alerts`
   - Recording rules in `prometheus/rules`

## Exchange Configuration

1. **TradingView Setup**
   - Configure API credentials in settings
   - Set up webhook endpoints
   - Configure rate limits

2. **Tradovate Setup**
   - Set up API access
   - Configure account credentials
   - Set trading permissions

## Development Tools

Recommended VS Code extensions:
- C# Dev Kit
- Docker
- REST Client
- GitLens

## Troubleshooting

Common issues and solutions:

1. **Database Connection**
   - Verify PostgreSQL connection strings
   - Check PostgreSQL service status
   - Confirm database user permissions
   - Verify database exists and is accessible

2. **Docker Issues**
   - Clear Docker cache
   - Check Docker logs
   - Verify port availability

3. **Build Errors**
   - Clear NuGet cache
   - Restore packages
   - Check SDK version

## Environment Variables

Key environment variables:

```bash
ASPNETCORE_ENVIRONMENT=Development
POSTGRES_CONNECTION=
EXCHANGE_API_KEY=
EXCHANGE_API_SECRET=
MONITORING_ENABLED=true
```

## Next Steps

1. Review the [Architecture](Architecture) documentation
2. Explore available [Components](Components)
3. Set up your development environment
4. Run the sample strategies
