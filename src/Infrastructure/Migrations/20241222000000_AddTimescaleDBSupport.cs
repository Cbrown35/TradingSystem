using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTimescaleDBSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable TimescaleDB extension
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS timescaledb CASCADE;");

            // Convert tables to hypertables
            migrationBuilder.Sql(@"
                SELECT create_hypertable('MarketData', 'Timestamp', 
                    chunk_time_interval => INTERVAL '1 day',
                    if_not_exists => TRUE);
                
                SELECT create_hypertable('Trades', 'OpenTime',
                    chunk_time_interval => INTERVAL '1 day',
                    if_not_exists => TRUE);
                
                SELECT create_hypertable('Orders', 'CreateTime',
                    chunk_time_interval => INTERVAL '1 day',
                    if_not_exists => TRUE);
            ");

            // Create continuous aggregate for market data
            migrationBuilder.Sql(@"
                CREATE MATERIALIZED VIEW IF NOT EXISTS market_data_1h
                WITH (timescaledb.continuous) AS
                SELECT time_bucket('1 hour', ""Timestamp"") AS bucket,
                       ""Symbol"",
                       first(""Open"", ""Timestamp"") AS open,
                       max(""High"") AS high,
                       min(""Low"") AS low,
                       last(""Close"", ""Timestamp"") AS close,
                       sum(""Volume"") AS volume
                FROM ""MarketData""
                GROUP BY bucket, ""Symbol""
                WITH NO DATA;

                SELECT add_continuous_aggregate_policy('market_data_1h',
                    start_offset => INTERVAL '1 month',
                    end_offset => INTERVAL '1 hour',
                    schedule_interval => INTERVAL '1 hour');
            ");

            // Add compression policies for older data
            migrationBuilder.Sql(@"
                -- Compress MarketData chunks older than 7 days
                ALTER TABLE MarketData SET (
                    timescaledb.compress,
                    timescaledb.compress_segmentby = 'Symbol'
                );

                SELECT add_compression_policy('MarketData', INTERVAL '7 days');

                -- Compress Trades chunks older than 30 days
                ALTER TABLE Trades SET (
                    timescaledb.compress,
                    timescaledb.compress_segmentby = 'Symbol,StrategyName'
                );

                SELECT add_compression_policy('Trades', INTERVAL '30 days');

                -- Compress Orders chunks older than 30 days
                ALTER TABLE Orders SET (
                    timescaledb.compress,
                    timescaledb.compress_segmentby = 'Symbol,StrategyName'
                );

                SELECT add_compression_policy('Orders', INTERVAL '30 days');
            ");

            // Add TimescaleDB-specific indexes
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_market_data_symbol_timestamp_desc ON MarketData (Symbol, Timestamp DESC);
                CREATE INDEX IF NOT EXISTS ix_trades_symbol_opentime_desc ON Trades (Symbol, OpenTime DESC);
                CREATE INDEX IF NOT EXISTS ix_orders_symbol_createtime_desc ON Orders (Symbol, CreateTime DESC);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove compression policies
            migrationBuilder.Sql(@"
                SELECT remove_compression_policy('MarketData', if_exists => true);
                SELECT remove_compression_policy('Trades', if_exists => true);
                SELECT remove_compression_policy('Orders', if_exists => true);
            ");

            // Remove continuous aggregate
            migrationBuilder.Sql("DROP MATERIALIZED VIEW IF EXISTS market_data_1h CASCADE;");

            // Note: We don't remove the TimescaleDB extension or convert hypertables back to regular tables
            // as this could result in data loss. Instead, we leave them as is for safety.
        }
    }
}
