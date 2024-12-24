using Microsoft.EntityFrameworkCore;
using TradingSystem.Common.Models;

namespace TradingSystem.Infrastructure.Data;

public class DevelopmentTradingContext : DbContext
{
    public DevelopmentTradingContext(DbContextOptions<DevelopmentTradingContext> options) : base(options)
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
                .HasColumnType("jsonb")
                .HasDefaultValue("{}");
            
            entity.Property(e => e.MetricsJson)
                .HasColumnType("jsonb")
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
                .HasColumnType("jsonb")
                .HasDefaultValue("{}");

            entity.Ignore(e => e.Parameters);
        });

        modelBuilder.Entity<Indicator>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            
            entity.Property(e => e.ParametersJson)
                .HasColumnType("jsonb")
                .HasDefaultValue("{}");
            
            entity.Property(e => e.ValuesJson)
                .HasColumnType("jsonb")
                .HasDefaultValue("{}");
            
            entity.Property(e => e.SettingsJson)
                .HasColumnType("jsonb")
                .HasDefaultValue("{}");
            
            entity.Property(e => e.DependenciesJson)
                .HasColumnType("jsonb")
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

            entity.Property(e => e.Indicators).HasColumnType("jsonb");
            entity.Property(e => e.Tags).HasColumnType("jsonb");
            entity.Property(e => e.Signals).HasColumnType("jsonb");
            entity.Property(e => e.RiskMetrics).HasColumnType("jsonb");

            entity.HasOne<Trade>()
                .WithMany()
                .HasForeignKey(e => e.ParentTradeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
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

            entity.Property(e => e.Indicators).HasColumnType("jsonb");
            entity.Property(e => e.CustomMetrics).HasColumnType("jsonb");
            entity.Property(e => e.OrderBook).HasColumnType("jsonb");
            entity.Property(e => e.RecentTrades).HasColumnType("jsonb");
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

            entity.Property(e => e.Tags).HasColumnType("jsonb");
        });

        modelBuilder.Entity<Theory>(entity =>
        {
            entity.HasKey(e => e.Name);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            
            entity.Property(e => e.Symbols).HasColumnType("jsonb");
            entity.Property(e => e.Parameters).HasColumnType("jsonb");
        });

        base.OnModelCreating(modelBuilder);
    }
}
