# Trading System Monitoring with Prometheus

This directory contains the Prometheus configuration for monitoring the trading system, including metrics collection, alerting rules, and notification templates.

## Directory Structure

```
prometheus/
├── prometheus.yml          # Main Prometheus configuration
├── alertmanager.yml       # Alert routing and notification config
├── rules/
│   └── recording_rules.yml # Metric aggregation rules
├── alerts/
│   └── trading_alerts.yml # Alert definitions
└── templates/             # Notification templates
    ├── email.tmpl
    ├── slack.tmpl
    └── pagerduty.tmpl
```

## Configuration Overview

### Metrics Collection

The system collects metrics from several components:
- Trading System Core (`localhost:5000`)
- Market Data Service (`localhost:5001`)
- Strategy Execution (`localhost:5002`)
- Risk Management (`localhost:5003`)
- Database Metrics (`localhost:9187`)
- Redis Metrics (`localhost:9121`)
- Node Metrics (`localhost:9100`)

### Recording Rules

Recording rules aggregate raw metrics into meaningful indicators:
- System health scores
- Trading performance metrics
- Risk exposure metrics
- Market data quality metrics
- Infrastructure metrics
- SLO compliance metrics

### Alert Categories

1. **System Health**
   - Component availability
   - Service degradation

2. **Trading Operations**
   - Trade failure rates
   - Significant losses
   - Strategy performance

3. **Risk Management**
   - Exposure limits
   - Margin usage
   - Position concentration

4. **Market Data**
   - Data latency
   - Update frequency
   - Data quality

5. **Infrastructure**
   - Resource utilization
   - Database connections
   - Cache performance

6. **SLO Compliance**
   - Availability metrics
   - Latency thresholds
   - Error budgets

## Alert Routing

Alerts are routed based on:
- Severity (critical/warning)
- Category (trading/risk/infrastructure/etc.)
- Team assignment

### Notification Channels

1. **PagerDuty**
   - Critical alerts
   - After-hours incidents
   - Service disruptions

2. **Slack**
   - Team-specific channels
   - Warning-level alerts
   - Status updates

3. **Email**
   - Daily summaries
   - Non-urgent notifications
   - Detailed reports

## Setup Instructions

1. Install Prometheus:
   ```bash
   docker pull prom/prometheus
   ```

2. Install Alertmanager:
   ```bash
   docker pull prom/alertmanager
   ```

3. Configure notification channels:
   - Update `alertmanager.yml` with your credentials
   - Set up team-specific routing
   - Configure notification templates

4. Start the services:
   ```bash
   docker-compose up -d
   ```

## Alert Severity Levels

- **Critical**
  - System down
  - Trading halted
  - Data integrity issues
  - Significant financial impact

- **Warning**
  - Performance degradation
  - Resource constraints
  - Approaching limits
  - Quality concerns

## Maintenance

### Adding New Metrics

1. Define the metric in the relevant exporter
2. Add scrape configuration in `prometheus.yml`
3. Create recording rules if needed
4. Update dashboards

### Creating New Alerts

1. Add alert definition in `trading_alerts.yml`
2. Define routing in `alertmanager.yml`
3. Update notification templates
4. Test the alert

### Updating Templates

1. Modify templates in `/templates`
2. Test with different alert scenarios
3. Verify notification formatting
4. Update documentation

## Best Practices

1. **Alert Design**
   - Clear, actionable alerts
   - Appropriate severity levels
   - Meaningful descriptions
   - Runbook links

2. **Notification Management**
   - Prevent alert fatigue
   - Group related alerts
   - Rate limiting
   - Proper escalation

3. **Metric Collection**
   - Relevant metrics only
   - Appropriate intervals
   - Resource consideration
   - Data retention

4. **Maintenance**
   - Regular testing
   - Template updates
   - Configuration reviews
   - Documentation updates

## Troubleshooting

### Common Issues

1. **Missing Metrics**
   - Check exporter status
   - Verify scrape config
   - Check network connectivity

2. **Alert Issues**
   - Verify alert rules
   - Check routing config
   - Test notification channels

3. **Performance Problems**
   - Review retention settings
   - Check resource usage
   - Optimize queries

### Debug Commands

```bash
# Check Prometheus config
promtool check config prometheus.yml

# Test alert rules
promtool check rules trading_alerts.yml

# Verify alertmanager config
amtool check-config alertmanager.yml

# Test alert routing
amtool alert add alertname=test severity=critical
```

## Support

For issues or questions:
- Create a ticket in the issue tracker
- Contact the SRE team
- Check runbooks in the wiki
