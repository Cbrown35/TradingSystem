using Microsoft.EntityFrameworkCore;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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

            entity.HasIndex(e => e.Symbol);
            entity.HasIndex(e => e.StrategyName);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.OpenTime);
            entity.HasIndex(e => e.CloseTime);
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
            entity.Property(e => e.OrderBookLevels).HasColumnType("jsonb");
            entity.Property(e => e.RecentTrades).HasColumnType("jsonb");

            entity.HasIndex(e => new { e.Symbol, e.Timestamp });
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.Interval);
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

            entity.Property(e => e.Tags).HasColumnType("jsonb");

            entity.HasIndex(e => e.Symbol);
            entity.HasIndex(e => e.CreateTime);
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
            
            entity.Property(e => e.Symbols).HasColumnType("jsonb");
            entity.Property(e => e.Parameters).HasColumnType("jsonb");
        });

        base.OnModelCreating(modelBuilder);
    }
}
