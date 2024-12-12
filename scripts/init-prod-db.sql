-- Initialize production database with security and performance optimizations

-- Create extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";
CREATE EXTENSION IF NOT EXISTS "btree_gin";
CREATE EXTENSION IF NOT EXISTS "pg_stat_statements";

-- Set production configurations
ALTER SYSTEM SET max_connections = '200';
ALTER SYSTEM SET shared_buffers = '1GB';
ALTER SYSTEM SET effective_cache_size = '3GB';
ALTER SYSTEM SET work_mem = '16MB';
ALTER SYSTEM SET maintenance_work_mem = '256MB';
ALTER SYSTEM SET random_page_cost = '1.1';
ALTER SYSTEM SET effective_io_concurrency = '200';
ALTER SYSTEM SET wal_buffers = '16MB';
ALTER SYSTEM SET checkpoint_completion_target = '0.9';
ALTER SYSTEM SET default_statistics_target = '100';

-- Create schemas with restricted access
CREATE SCHEMA IF NOT EXISTS trading AUTHORIZATION postgres;
CREATE SCHEMA IF NOT EXISTS market_data AUTHORIZATION postgres;
CREATE SCHEMA IF NOT EXISTS analytics AUTHORIZATION postgres;

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
END$$;

-- Create optimized indexes
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_market_data_symbol_timestamp 
    ON market_data.market_data (symbol, timestamp) 
    WITH (fillfactor = 90);

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_trades_symbol_status 
    ON trading.trades (symbol, status) 
    INCLUDE (strategy_name, realized_pnl)
    WITH (fillfactor = 90);

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_orders_symbol_status 
    ON trading.orders (symbol, status) 
    INCLUDE (strategy_name, quantity, price)
    WITH (fillfactor = 90);

-- Create partitioning function
CREATE OR REPLACE FUNCTION create_market_data_partition(
    start_date DATE,
    end_date DATE
) RETURNS void AS $$
BEGIN
    EXECUTE format(
        'CREATE TABLE IF NOT EXISTS market_data.market_data_%s_%s 
         PARTITION OF market_data.market_data 
         FOR VALUES FROM (%L) TO (%L)',
        to_char(start_date, 'YYYY_MM'),
        to_char(end_date, 'YYYY_MM'),
        start_date,
        end_date
    );
END;
$$ LANGUAGE plpgsql;

-- Create audit function
CREATE OR REPLACE FUNCTION audit.log_change() RETURNS trigger AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        INSERT INTO audit.audit_log (
            table_name,
            operation,
            new_data,
            changed_by
        ) VALUES (
            TG_TABLE_NAME,
            TG_OP,
            row_to_json(NEW),
            current_user
        );
        RETURN NEW;
    ELSIF TG_OP = 'UPDATE' THEN
        INSERT INTO audit.audit_log (
            table_name,
            operation,
            old_data,
            new_data,
            changed_by
        ) VALUES (
            TG_TABLE_NAME,
            TG_OP,
            row_to_json(OLD),
            row_to_json(NEW),
            current_user
        );
        RETURN NEW;
    ELSIF TG_OP = 'DELETE' THEN
        INSERT INTO audit.audit_log (
            table_name,
            operation,
            old_data,
            changed_by
        ) VALUES (
            TG_TABLE_NAME,
            TG_OP,
            row_to_json(OLD),
            current_user
        );
        RETURN OLD;
    END IF;
END;
$$ LANGUAGE plpgsql;

-- Create backup functions
CREATE OR REPLACE FUNCTION maintenance.backup_table(
    schema_name text,
    table_name text,
    backup_path text
) RETURNS void AS $$
BEGIN
    EXECUTE format(
        'COPY %I.%I TO %L WITH (FORMAT CSV, HEADER)',
        schema_name,
        table_name,
        backup_path
    );
END;
$$ LANGUAGE plpgsql;

-- Create maintenance functions
CREATE OR REPLACE FUNCTION maintenance.analyze_tables() RETURNS void AS $$
BEGIN
    ANALYZE VERBOSE trading.trades;
    ANALYZE VERBOSE trading.orders;
    ANALYZE VERBOSE market_data.market_data;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION maintenance.vacuum_tables() RETURNS void AS $$
BEGIN
    VACUUM ANALYZE trading.trades;
    VACUUM ANALYZE trading.orders;
    VACUUM ANALYZE market_data.market_data;
END;
$$ LANGUAGE plpgsql;

-- Create roles with least privilege
DO $$
BEGIN
    -- Trading application role
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'trading_app') THEN
        CREATE ROLE trading_app WITH LOGIN PASSWORD NULL;
    END IF;

    -- Read-only analytics role
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'trading_analytics') THEN
        CREATE ROLE trading_analytics WITH LOGIN PASSWORD NULL;
    END IF;

    -- Maintenance role
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'trading_maintenance') THEN
        CREATE ROLE trading_maintenance WITH LOGIN PASSWORD NULL;
    END IF;
END$$;

-- Grant permissions
GRANT USAGE ON SCHEMA trading TO trading_app;
GRANT USAGE ON SCHEMA market_data TO trading_app;
GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA trading TO trading_app;
GRANT SELECT, INSERT ON ALL TABLES IN SCHEMA market_data TO trading_app;

GRANT USAGE ON SCHEMA analytics TO trading_analytics;
GRANT SELECT ON ALL TABLES IN SCHEMA trading TO trading_analytics;
GRANT SELECT ON ALL TABLES IN SCHEMA market_data TO trading_analytics;
GRANT SELECT ON ALL TABLES IN SCHEMA analytics TO trading_analytics;

GRANT USAGE ON SCHEMA maintenance TO trading_maintenance;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA maintenance TO trading_maintenance;

-- Create monitoring views
CREATE OR REPLACE VIEW analytics.system_performance AS
SELECT 
    schemaname,
    relname,
    seq_scan,
    seq_tup_read,
    idx_scan,
    idx_tup_fetch,
    n_tup_ins,
    n_tup_upd,
    n_tup_del,
    n_live_tup,
    n_dead_tup,
    last_vacuum,
    last_autovacuum,
    last_analyze,
    last_autoanalyze
FROM 
    pg_stat_user_tables;

-- Set up table partitioning
ALTER TABLE market_data.market_data
    PARTITION BY RANGE (timestamp);

-- Create initial partitions
SELECT create_market_data_partition(
    date_trunc('month', CURRENT_DATE),
    date_trunc('month', CURRENT_DATE + interval '1 month')
);

-- Create maintenance procedures
CREATE OR REPLACE PROCEDURE maintenance.daily_maintenance()
LANGUAGE plpgsql
AS $$
BEGIN
    -- Analyze tables
    CALL maintenance.analyze_tables();
    
    -- Create next month's partition if needed
    CALL maintenance.create_next_partition();
    
    -- Archive old data
    CALL maintenance.archive_old_data();
    
    -- Update statistics
    CALL maintenance.update_statistics();
END;
$$;

-- Set up security policies
ALTER TABLE trading.trades ENABLE ROW LEVEL SECURITY;
ALTER TABLE trading.orders ENABLE ROW LEVEL SECURITY;

CREATE POLICY trades_access_policy ON trading.trades
    USING (current_user = 'trading_app' OR current_user = 'postgres');

CREATE POLICY orders_access_policy ON trading.orders
    USING (current_user = 'trading_app' OR current_user = 'postgres');

-- Create performance indexes
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_market_data_timestamp_brin
    ON market_data.market_data USING brin (timestamp)
    WITH (pages_per_range = 128);

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_trades_performance
    ON trading.trades (strategy_name, symbol, status)
    INCLUDE (realized_pnl, commission, slippage)
    WHERE status = 'Closed';

-- Set up monitoring
CREATE EXTENSION IF NOT EXISTS pg_stat_statements;
ALTER SYSTEM SET pg_stat_statements.max = 10000;
ALTER SYSTEM SET pg_stat_statements.track = 'all';
