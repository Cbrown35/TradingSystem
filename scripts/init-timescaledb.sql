-- Enable TimescaleDB extension
CREATE EXTENSION IF NOT EXISTS timescaledb;

-- Create market data hypertable
CREATE TABLE IF NOT EXISTS market_data (
    symbol VARCHAR NOT NULL,
    timestamp TIMESTAMPTZ NOT NULL,
    open DECIMAL NOT NULL,
    high DECIMAL NOT NULL,
    low DECIMAL NOT NULL,
    close DECIMAL NOT NULL,
    volume DECIMAL NOT NULL,
    quote_volume DECIMAL,
    open_interest DECIMAL,
    bid_price DECIMAL,
    ask_price DECIMAL,
    bid_size DECIMAL,
    ask_size DECIMAL,
    vwap DECIMAL,
    number_of_trades INTEGER,
    interval VARCHAR,
    indicators JSONB DEFAULT '{}',
    custom_metrics JSONB DEFAULT '{}',
    market_condition VARCHAR,
    imbalance_ratio DECIMAL,
    taker_buy_volume DECIMAL,
    taker_sell_volume DECIMAL,
    cumulative_delta DECIMAL,
    volatility DECIMAL,
    relative_volume DECIMAL,
    spread_percentage DECIMAL,
    liquidity_score DECIMAL,
    order_book JSONB DEFAULT '[]',
    recent_trades JSONB DEFAULT '[]'
);

-- Convert to hypertable
SELECT create_hypertable('market_data', 'timestamp');

-- Create trades table
CREATE TABLE IF NOT EXISTS trades (
    id UUID PRIMARY KEY,
    symbol VARCHAR NOT NULL,
    strategy_name VARCHAR NOT NULL,
    direction VARCHAR NOT NULL,
    entry_price DECIMAL NOT NULL,
    exit_price DECIMAL,
    quantity DECIMAL NOT NULL,
    stop_loss DECIMAL,
    take_profit DECIMAL,
    status VARCHAR NOT NULL,
    open_time TIMESTAMPTZ NOT NULL,
    close_time TIMESTAMPTZ,
    realized_pnl DECIMAL,
    unrealized_pnl DECIMAL,
    commission DECIMAL,
    slippage DECIMAL,
    drawdown_from_peak DECIMAL,
    risk_reward_ratio DECIMAL,
    return_on_investment DECIMAL,
    notes TEXT,
    market_condition VARCHAR,
    setup_type VARCHAR,
    indicators JSONB DEFAULT '{}',
    tags JSONB DEFAULT '[]',
    signals JSONB DEFAULT '[]',
    risk_metrics JSONB DEFAULT '{}',
    parent_trade_id UUID
);

-- Create index on trades timestamp
CREATE INDEX IF NOT EXISTS idx_trades_open_time ON trades(open_time DESC);

-- Create compression policy for market data
SELECT add_compression_policy('market_data', INTERVAL '7 days');

-- Create retention policy for market data (adjust as needed)
SELECT add_retention_policy('market_data', INTERVAL '1 year');

-- Grant permissions
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO postgres;
