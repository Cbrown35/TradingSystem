using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TradingSystem.Common.Models;

namespace TradingSystem.Infrastructure.Data;

public class TradingContext : DbContext
{
    public TradingContext(DbContextOptions<TradingContext> options) : base(options)
    {
    }

    public DbSet<Trade> Trades { get; set; } = null!;
    public DbSet<Theory> Theories { get; set; } = null!;
    public DbSet<MarketData> MarketData { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<Indicator> Indicators { get; set; } = null!;
    public DbSet<Signal> Signals { get; set; } = null!;
    public DbSet<SignalCondition> SignalConditions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Signal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Expression).HasMaxLength(1000);
            
            entity.Property(e => e.ParametersJson)
                .HasColumnType("nvarchar(max)")
                .HasDefaultValue("{}");
            
            entity.Property(e => e.MetricsJson)
                .HasColumnType("nvarchar(max)")
                .HasDefaultValue("{}");

            entity.HasMany(e => e.Conditions)
                .WithOne()
                .HasForeignKey(e => e.SignalId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Ignore(e => e.Parameters);
            entity.Ignore(e => e.Metrics);
        });

        modelBuilder.Entity<SignalCondition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Expression).HasMaxLength(1000);
            entity.Property(e => e.SignalId).IsRequired();
            
            entity.Property(e => e.ParametersJson)
                .HasColumnType("nvarchar(max)")
                .HasDefaultValue("{}");

            entity.Ignore(e => e.Parameters);
        });

        modelBuilder.Entity<Indicator>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            
            entity.Property(e => e.ParametersJson)
                .HasColumnType("nvarchar(max)")
                .HasDefaultValue("{}");
            
            entity.Property(e => e.ValuesJson)
                .HasColumnType("nvarchar(max)")
                .HasDefaultValue("{}");
            
            entity.Property(e => e.SettingsJson)
                .HasColumnType("nvarchar(max)")
                .HasDefaultValue("{}");
            
            entity.Property(e => e.DependenciesJson)
                .HasColumnType("nvarchar(max)")
                .HasDefaultValue("[]");

            entity.Ignore(e => e.Parameters);
            entity.Ignore(e => e.Values);
            entity.Ignore(e => e.Settings);
            entity.Ignore(e => e.Dependencies);

            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Type);
        });

        modelBuilder.Entity<Trade>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Symbol).IsRequired().HasMaxLength(20);
            entity.Property(e => e.StrategyName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntryPrice).HasPrecision(18, 8);
            entity.Property(e => e.ExitPrice).HasPrecision(18, 8);
            entity.Property(e => e.Quantity).HasPrecision(18, 8);
            entity.Property(e => e.StopLoss).HasPrecision(18, 8);
            entity.Property(e => e.TakeProfit).HasPrecision(18, 8);
            entity.Property(e => e.RealizedPnL).HasPrecision(18, 8);
            entity.Property(e => e.Commission).HasPrecision(18, 8);
            entity.Property(e => e.Slippage).HasPrecision(18, 8);
            entity.Property(e => e.UnrealizedPnL).HasPrecision(18, 8);
            entity.Property(e => e.DrawdownFromPeak).HasPrecision(18, 8);
            entity.Property(e => e.RiskRewardRatio).HasPrecision(18, 8);
            entity.Property(e => e.ReturnOnInvestment).HasPrecision(18, 8);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.MarketCondition).HasMaxLength(50);
            entity.Property(e => e.SetupType).HasMaxLength(50);

            entity.Property(e => e.Indicators)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, decimal>>(v, (JsonSerializerOptions)null) ?? new Dictionary<string, decimal>())
                .HasColumnType("nvarchar(max)");
            
            entity.Property(e => e.Tags)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null) ?? new List<string>())
                .HasColumnType("nvarchar(max)");
            
            entity.Property(e => e.Signals)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<Signal>>(v, (JsonSerializerOptions)null) ?? new List<Signal>())
                .HasColumnType("nvarchar(max)");
            
            entity.Property(e => e.RiskMetrics)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, decimal>>(v, (JsonSerializerOptions)null) ?? new Dictionary<string, decimal>())
                .HasColumnType("nvarchar(max)");

            entity.HasOne<Trade>()
                .WithMany()
                .HasForeignKey(e => e.ParentTradeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // TimescaleDB time-series indexes
            entity.HasIndex(e => e.OpenTime)
                .HasDatabaseName("ix_trades_open_time_desc")
                .IsDescending();
            entity.HasIndex(e => e.CloseTime)
                .HasDatabaseName("ix_trades_close_time_desc")
                .IsDescending();
            entity.HasIndex(e => new { e.Symbol, e.OpenTime })
                .HasDatabaseName("ix_trades_symbol_open_time");
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.StrategyName);
        });

        modelBuilder.Entity<MarketData>(entity =>
        {
            entity.HasKey(e => new { e.Symbol, e.Timestamp });
            entity.Property(e => e.Symbol).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Open).HasPrecision(18, 8);
            entity.Property(e => e.High).HasPrecision(18, 8);
            entity.Property(e => e.Low).HasPrecision(18, 8);
            entity.Property(e => e.Close).HasPrecision(18, 8);
            entity.Property(e => e.Volume).HasPrecision(18, 8);
            entity.Property(e => e.QuoteVolume).HasPrecision(18, 8);
            entity.Property(e => e.OpenInterest).HasPrecision(18, 8);
            entity.Property(e => e.BidPrice).HasPrecision(18, 8);
            entity.Property(e => e.AskPrice).HasPrecision(18, 8);
            entity.Property(e => e.BidSize).HasPrecision(18, 8);
            entity.Property(e => e.AskSize).HasPrecision(18, 8);
            entity.Property(e => e.VWAP).HasPrecision(18, 8);
            entity.Property(e => e.ImbalanceRatio).HasPrecision(18, 8);
            entity.Property(e => e.TakerBuyVolume).HasPrecision(18, 8);
            entity.Property(e => e.TakerSellVolume).HasPrecision(18, 8);
            entity.Property(e => e.CumulativeDelta).HasPrecision(18, 8);
            entity.Property(e => e.Volatility).HasPrecision(18, 8);
            entity.Property(e => e.RelativeVolume).HasPrecision(18, 8);
            entity.Property(e => e.SpreadPercentage).HasPrecision(18, 8);
            entity.Property(e => e.LiquidityScore).HasPrecision(18, 8);

            entity.Property(e => e.Indicators)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, decimal>>(v, (JsonSerializerOptions)null) ?? new Dictionary<string, decimal>())
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.CustomMetrics)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, decimal>>(v, (JsonSerializerOptions)null) ?? new Dictionary<string, decimal>())
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.OrderBook)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<OrderBookLevel>>(v, (JsonSerializerOptions)null) ?? new List<OrderBookLevel>())
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.RecentTrades)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<Trade>>(v, (JsonSerializerOptions)null) ?? new List<Trade>())
                .HasColumnType("nvarchar(max)");

            // TimescaleDB time-series indexes
            entity.HasIndex(e => e.Timestamp)
                .HasDatabaseName("ix_market_data_timestamp_desc")
                .IsDescending();
            entity.HasIndex(e => new { e.Symbol, e.Timestamp })
                .HasDatabaseName("ix_market_data_symbol_timestamp");
            entity.HasIndex(e => new { e.Symbol, e.Interval, e.Timestamp })
                .HasDatabaseName("ix_market_data_symbol_interval_timestamp");
            entity.HasIndex(e => e.MarketCondition);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Symbol).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ClientOrderId).HasMaxLength(50);
            entity.Property(e => e.ExchangeOrderId).HasMaxLength(50);
            entity.Property(e => e.StrategyName).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.ErrorMessage).HasMaxLength(500);
            entity.Property(e => e.TriggerCondition).HasMaxLength(200);

            entity.Property(e => e.Quantity).HasPrecision(18, 8);
            entity.Property(e => e.Price).HasPrecision(18, 8);
            entity.Property(e => e.StopPrice).HasPrecision(18, 8);
            entity.Property(e => e.LimitPrice).HasPrecision(18, 8);
            entity.Property(e => e.AverageFilledPrice).HasPrecision(18, 8);
            entity.Property(e => e.FilledQuantity).HasPrecision(18, 8);
            entity.Property(e => e.RemainingQuantity).HasPrecision(18, 8);
            entity.Property(e => e.Commission).HasPrecision(18, 8);
            entity.Property(e => e.Slippage).HasPrecision(18, 8);

            entity.Property(e => e.Tags)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions)null) ?? new Dictionary<string, string>())
                .HasColumnType("nvarchar(max)");

            // TimescaleDB time-series indexes
            entity.HasIndex(e => e.CreateTime)
                .HasDatabaseName("ix_orders_create_time_desc")
                .IsDescending();
            entity.HasIndex(e => new { e.Symbol, e.CreateTime })
                .HasDatabaseName("ix_orders_symbol_create_time");
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ClientOrderId);
            entity.HasIndex(e => e.ExchangeOrderId);
            entity.HasIndex(e => e.TradeId);
            entity.HasIndex(e => e.StrategyName);
        });

        modelBuilder.Entity<Theory>(entity =>
        {
            entity.HasKey(e => e.Name);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            
            entity.Property(e => e.Symbols)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null) ?? new List<string>())
                .HasColumnType("nvarchar(max)");
            
            entity.Property(e => e.Parameters)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, decimal>>(v, (JsonSerializerOptions)null) ?? new Dictionary<string, decimal>())
                .HasColumnType("nvarchar(max)");
        });

        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // No additional configuration needed for in-memory database
        base.OnConfiguring(optionsBuilder);
    }

    public virtual async Task CreateHypertablesAsync()
    {
        // Create hypertables for time-series data
        await Database.ExecuteSqlRawAsync(@"
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

        // Create continuous aggregates for different time intervals
        await Database.ExecuteSqlRawAsync(@"
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
    }
}
