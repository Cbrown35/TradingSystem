-- Initialize development database

-- Create extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";
CREATE EXTENSION IF NOT EXISTS "btree_gin";

-- Create schemas
CREATE SCHEMA IF NOT EXISTS trading;
CREATE SCHEMA IF NOT EXISTS market_data;
CREATE SCHEMA IF NOT EXISTS analytics;

-- Set search path
SET search_path TO trading, market_data, analytics, public;

-- Create custom types
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'trade_status') THEN
        CREATE TYPE trade_status AS ENUM (
            'Open',
            'Closed',
            'Cancelled',
            'Pending',
            'PartiallyFilled',
            'Error'
        );
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'order_type') THEN
        CREATE TYPE order_type AS ENUM (
            'Market',
            'Limit',
            'StopLoss',
            'StopLossLimit',
            'TakeProfit',
            'TakeProfitLimit',
            'TrailingStop',
            'TrailingStopLimit',
            'LimitMaker',
            'ConditionalLimit',
            'ConditionalMarket'
        );
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'order_side') THEN
        CREATE TYPE order_side AS ENUM (
            'Buy',
            'Sell'
        );
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'time_frame') THEN
        CREATE TYPE time_frame AS ENUM (
            'Tick',
            'OneSecond',
            'OneMinute',
            'ThreeMinutes',
            'FiveMinutes',
            'FifteenMinutes',
            'ThirtyMinutes',
            'OneHour',
            'TwoHours',
            'FourHours',
            'SixHours',
            'EightHours',
            'TwelveHours',
            'OneDay',
            'ThreeDays',
            'OneWeek',
            'OneMonth'
        );
    END IF;
END$$;

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_market_data_symbol_timestamp 
    ON market_data.market_data (symbol, timestamp);

CREATE INDEX IF NOT EXISTS idx_trades_symbol 
    ON trading.trades (symbol);

CREATE INDEX IF NOT EXISTS idx_trades_strategy 
    ON trading.trades (strategy_name);

CREATE INDEX IF NOT EXISTS idx_trades_status 
    ON trading.trades (status);

CREATE INDEX IF NOT EXISTS idx_orders_symbol 
    ON trading.orders (symbol);

CREATE INDEX IF NOT EXISTS idx_orders_status 
    ON trading.orders (status);

-- Create functions
CREATE OR REPLACE FUNCTION update_modified_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.modified_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Create development user and grant permissions
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'trading_dev') THEN
        CREATE ROLE trading_dev WITH LOGIN PASSWORD 'dev_password';
    END IF;
END$$;

GRANT USAGE ON SCHEMA trading TO trading_dev;
GRANT USAGE ON SCHEMA market_data TO trading_dev;
GRANT USAGE ON SCHEMA analytics TO trading_dev;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA trading TO trading_dev;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA market_data TO trading_dev;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA analytics TO trading_dev;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA trading TO trading_dev;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA market_data TO trading_dev;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA analytics TO trading_dev;

-- Create test data
INSERT INTO market_data.symbols (symbol, name, exchange, is_active)
VALUES 
    ('BTCUSDT', 'Bitcoin/USDT', 'Binance', true),
    ('ETHUSDT', 'Ethereum/USDT', 'Binance', true),
    ('AAPL', 'Apple Inc.', 'NASDAQ', true),
    ('MSFT', 'Microsoft Corporation', 'NASDAQ', true)
ON CONFLICT (symbol) DO NOTHING;

-- Create views
CREATE OR REPLACE VIEW analytics.trade_performance AS
SELECT 
    t.symbol,
    t.strategy_name,
    COUNT(*) as total_trades,
    SUM(CASE WHEN realized_pnl > 0 THEN 1 ELSE 0 END) as winning_trades,
    SUM(CASE WHEN realized_pnl <= 0 THEN 1 ELSE 0 END) as losing_trades,
    AVG(realized_pnl) as avg_pnl,
    SUM(realized_pnl) as total_pnl,
    AVG(CASE WHEN realized_pnl > 0 THEN realized_pnl ELSE NULL END) as avg_win,
    AVG(CASE WHEN realized_pnl <= 0 THEN realized_pnl ELSE NULL END) as avg_loss,
    MAX(realized_pnl) as max_win,
    MIN(realized_pnl) as max_loss,
    AVG(EXTRACT(EPOCH FROM (close_time - open_time))) as avg_duration_seconds
FROM trading.trades t
WHERE t.status = 'Closed'
GROUP BY t.symbol, t.strategy_name;

-- Create materialized views
CREATE MATERIALIZED VIEW IF NOT EXISTS analytics.daily_performance AS
SELECT 
    DATE_TRUNC('day', t.close_time) as trade_date,
    t.symbol,
    t.strategy_name,
    COUNT(*) as trades,
    SUM(realized_pnl) as total_pnl,
    AVG(realized_pnl) as avg_pnl,
    SUM(commission) as total_commission,
    SUM(slippage) as total_slippage
FROM trading.trades t
WHERE t.status = 'Closed'
GROUP BY DATE_TRUNC('day', t.close_time), t.symbol, t.strategy_name;

-- Create refresh function
CREATE OR REPLACE FUNCTION refresh_materialized_views()
RETURNS void AS $$
BEGIN
    REFRESH MATERIALIZED VIEW CONCURRENTLY analytics.daily_performance;
END;
$$ LANGUAGE plpgsql;

-- Create refresh trigger
CREATE OR REPLACE FUNCTION trigger_refresh_materialized_views()
RETURNS trigger AS $$
BEGIN
    PERFORM refresh_materialized_views();
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER refresh_materialized_views_trigger
AFTER INSERT OR UPDATE OR DELETE ON trading.trades
FOR EACH STATEMENT
EXECUTE FUNCTION trigger_refresh_materialized_views();

-- Add comments
COMMENT ON SCHEMA trading IS 'Schema for trading-related tables';
COMMENT ON SCHEMA market_data IS 'Schema for market data storage';
COMMENT ON SCHEMA analytics IS 'Schema for analytics and reporting';

-- Create backup function
CREATE OR REPLACE FUNCTION backup_trading_data(backup_path TEXT)
RETURNS void AS $$
BEGIN
    EXECUTE format('COPY (
        SELECT * FROM trading.trades 
        WHERE close_time >= CURRENT_DATE - INTERVAL ''7 days''
    ) TO %L', backup_path);
END;
$$ LANGUAGE plpgsql;
