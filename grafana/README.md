# Trading System Grafana Configuration

This directory contains the Grafana configuration for the Trading System, including dashboard definitions, data source configurations, and provisioning settings.

## Directory Structure

```
grafana/
├── provisioning/
│   ├── dashboards/
│   │   ├── dashboards.yml     # Dashboard provisioning config
│   │   └── trading_dashboard.json  # Trading system dashboard
│   └── datasources/
│       └── prometheus.yml     # Data source configurations
├── grafana.ini               # Main Grafana configuration
└── README.md                 # This documentation
```

## Dashboards

### Trading System Dashboard
- Real-time trading performance metrics
- System health monitoring
- Risk management indicators
- Strategy performance analysis

#### Panels

1. Trading Performance
   - Daily P&L
   - Win Rate
   - Trade Volume
   - Position Size

2. Risk Metrics
   - Portfolio Exposure
   - Current Drawdown
   - Risk Limits
   - Position Concentration

3. System Health
   - Error Rates
   - Trade Latency
   - System Resources
   - Database Performance

4. Market Data
   - Data Delay
   - Update Frequency
   - Market Conditions
   - Price Movements

## Data Sources

### Prometheus
- Trading metrics
- System metrics
- Application metrics
- Alert status

### PostgreSQL
- Historical trade data
- Strategy performance
- Market data analysis
- Risk calculations

### Redis
- Real-time market data
- Cache statistics
- Queue metrics
- Session data

### Elasticsearch
- Application logs
- Trading logs
- Error tracking
- Audit trail

## Configuration

### Data Source Setup

1. Prometheus
```yaml
name: Prometheus
type: prometheus
url: http://prometheus:9090
access: proxy
isDefault: true
```

2. PostgreSQL
```yaml
name: PostgreSQL
type: postgres
url: postgres:5432
database: tradingsystem
user: ${DB_USER}
secureJsonData:
  password: ${DB_PASSWORD}
```

3. Redis
```yaml
name: Redis
type: redis-datasource
url: redis://redis:6379
```

### Dashboard Provisioning

```yaml
apiVersion: 1
providers:
  - name: 'Trading System'
    folder: 'Trading'
    type: file
    options:
      path: /etc/grafana/provisioning/dashboards
```

## Variables

### Global Variables
- `$timeRange`: Time range selector
- `$interval`: Time interval for aggregations
- `$strategy`: Strategy selector
- `$symbol`: Trading symbol selector

### Dashboard Variables
- `$threshold`: Alert thresholds
- `$percentile`: Performance percentiles
- `$windowSize`: Moving average windows
- `$riskLevel`: Risk tolerance levels

## Alerts

### Trading Alerts
- P&L thresholds
- Risk limit breaches
- Strategy performance
- Position limits

### System Alerts
- Error rates
- Latency thresholds
- Resource utilization
- Data quality

## User Management

### Roles
1. Admin
   - Full access
   - Configuration changes
   - User management

2. Trading Manager
   - View all dashboards
   - Configure alerts
   - Modify thresholds

3. Trader
   - View trading dashboards
   - Personal performance metrics
   - Strategy analysis

4. Analyst
   - View market data
   - Performance analysis
   - Historical data access

## Best Practices

### Dashboard Design
1. Layout
   - Logical grouping of panels
   - Consistent spacing
   - Clear hierarchy
   - Mobile-friendly design

2. Panels
   - Clear titles
   - Appropriate visualizations
   - Helpful tooltips
   - Units and formats

3. Performance
   - Efficient queries
   - Appropriate refresh rates
   - Template variable optimization
   - Cache utilization

### Query Optimization

1. Prometheus
```promql
# Good
rate(http_requests_total[5m])

# Better - with label matching
rate(http_requests_total{status=~"5.."}[5m])

# Best - with recording rule
job:http_requests:rate5m
```

2. PostgreSQL
```sql
-- Good
SELECT date_trunc('hour', timestamp), count(*)
FROM trades
WHERE timestamp > now() - interval '24 hours'
GROUP BY 1;

-- Better - with materialized view
SELECT * FROM hourly_trade_stats
WHERE timestamp > now() - interval '24 hours';
```

## Maintenance

### Backup
```bash
# Backup dashboards
grafana-cli admin export-dashboards > dashboards-backup.json

# Backup data sources
cp /etc/grafana/provisioning/datasources/* backup/
```

### Restore
```bash
# Restore dashboards
grafana-cli admin import-dashboards < dashboards-backup.json

# Restore data sources
cp backup/* /etc/grafana/provisioning/datasources/
```

### Cleanup
```bash
# Clean up old snapshots
grafana-cli admin cleanup-snapshots

# Clean up old data
grafana-cli admin cleanup-expired-data
```

## Troubleshooting

### Common Issues

1. Data Source Connection
```bash
# Test Prometheus connection
curl -X GET http://prometheus:9090/api/v1/status/config

# Test PostgreSQL connection
psql -h postgres -U ${DB_USER} -d tradingsystem -c "SELECT 1"
```

2. Dashboard Loading
- Check browser console
- Verify data source health
- Review query performance
- Check permissions

3. Alert Notifications
- Verify notification channels
- Check alert conditions
- Review alert history
- Test notification delivery

## Security

### Authentication
- LDAP/Active Directory integration
- OAuth configuration
- Two-factor authentication
- API key management

### Authorization
- Role-based access control
- Team-based permissions
- Dashboard permissions
- Data source restrictions

### Audit
- User activity logging
- Configuration changes
- Alert history
- Access logs

## Metrics Collection

### Custom Metrics
```javascript
// Trading metrics
custom_metric{
  strategy="moving_average",
  symbol="BTCUSDT",
  type="trade_execution"
} 

// System metrics
custom_metric{
  component="order_processor",
  instance="trading-1",
  type="latency"
}
```

### Dashboard Annotations
```javascript
// Market events
{
  "type": "market_event",
  "tags": ["news", "earnings", "volatility"],
  "text": "Market event description"
}

// System events
{
  "type": "system_event",
  "tags": ["deployment", "config_change", "restart"],
  "text": "System event description"
}
