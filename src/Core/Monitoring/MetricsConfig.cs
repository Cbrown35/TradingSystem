using Prometheus;

namespace TradingSystem.Core.Monitoring;

public static class MetricsConfig
{
    // System metrics
    public static readonly Counter HealthCheckRequests = Metrics
        .CreateCounter("healthcheck_requests_total", "Number of health check requests",
            new CounterConfiguration
            {
                LabelNames = new[] { "path", "method", "status_code" }
            });

    public static readonly Histogram HealthCheckDuration = Metrics
        .CreateHistogram("healthcheck_duration_seconds", "Health check duration in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "path", "method" },
                Buckets = new[] { .005, .01, .025, .05, .075, .1, .25, .5, .75, 1, 2.5, 5, 7.5, 10 }
            });

    public static readonly Counter HealthCheckErrors = Metrics
        .CreateCounter("healthcheck_errors_total", "Number of health check errors",
            new CounterConfiguration
            {
                LabelNames = new[] { "path", "status_code", "error" }
            });

    // Trading metrics
    public static readonly Gauge ActiveTrades = Metrics
        .CreateGauge("trading_active_trades", "Number of currently active trades",
            new GaugeConfiguration
            {
                LabelNames = new[] { "strategy", "market" }
            });

    public static readonly Counter TradeExecutions = Metrics
        .CreateCounter("trading_executions_total", "Number of trade executions",
            new CounterConfiguration
            {
                LabelNames = new[] { "strategy", "market", "type", "result" }
            });

    public static readonly Histogram TradeLatency = Metrics
        .CreateHistogram("trading_latency_seconds", "Trade execution latency in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "strategy", "market", "type" },
                Buckets = new[] { .001, .005, .01, .025, .05, .075, .1, .25, .5, .75, 1 }
            });

    // Market data metrics
    public static readonly Counter MarketDataUpdates = Metrics
        .CreateCounter("market_data_updates_total", "Number of market data updates",
            new CounterConfiguration
            {
                LabelNames = new[] { "market", "type" }
            });

    public static readonly Histogram MarketDataLatency = Metrics
        .CreateHistogram("market_data_latency_seconds", "Market data latency in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "market", "type" },
                Buckets = new[] { .001, .005, .01, .025, .05, .075, .1, .25, .5 }
            });

    // Risk metrics
    public static readonly Gauge RiskExposure = Metrics
        .CreateGauge("risk_exposure_total", "Total risk exposure",
            new GaugeConfiguration
            {
                LabelNames = new[] { "market", "strategy" }
            });

    public static readonly Gauge MarginUsage = Metrics
        .CreateGauge("margin_usage_ratio", "Margin usage ratio",
            new GaugeConfiguration
            {
                LabelNames = new[] { "market", "strategy" }
            });

    // Infrastructure metrics
    public static readonly Counter DatabaseOperations = Metrics
        .CreateCounter("database_operations_total", "Number of database operations",
            new CounterConfiguration
            {
                LabelNames = new[] { "operation", "table", "result" }
            });

    public static readonly Histogram DatabaseLatency = Metrics
        .CreateHistogram("database_latency_seconds", "Database operation latency in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "operation", "table" },
                Buckets = new[] { .001, .005, .01, .025, .05, .075, .1, .25, .5 }
            });

    public static readonly Gauge CacheSize = Metrics
        .CreateGauge("cache_size_bytes", "Cache size in bytes",
            new GaugeConfiguration
            {
                LabelNames = new[] { "cache" }
            });

    public static readonly Counter CacheOperations = Metrics
        .CreateCounter("cache_operations_total", "Number of cache operations",
            new CounterConfiguration
            {
                LabelNames = new[] { "operation", "cache", "result" }
            });

    // Initialize all metrics
    public static void InitializeMetrics()
    {
        // Pre-create metric instances with common labels
        HealthCheckRequests.WithLabels("health", "GET", "200");
        TradeExecutions.WithLabels("default", "all", "market", "success");
        MarketDataUpdates.WithLabels("all", "price");
        DatabaseOperations.WithLabels("query", "trades", "success");
        CacheOperations.WithLabels("get", "trading", "hit");
    }
}
