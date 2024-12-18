namespace TradingSystem.Common.Models;

public class Trade
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string StrategyName { get; set; } = string.Empty;
    public TradeDirection Direction { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal? ExitPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal? StopLoss { get; set; }
    public decimal? TakeProfit { get; set; }
    public TradeStatus Status { get; set; }
    public DateTime OpenTime { get; set; }
    public DateTime? CloseTime { get; set; }
    public decimal? RealizedPnL { get; set; }
    public decimal? UnrealizedPnL { get; set; }
    public decimal? Commission { get; set; }
    public decimal? Slippage { get; set; }
    public decimal? DrawdownFromPeak { get; set; }
    public decimal? RiskRewardRatio { get; set; }
    public decimal? ReturnOnInvestment { get; set; }
    public string? Notes { get; set; }
    public MarketCondition MarketCondition { get; set; }
    public string? SetupType { get; set; }
    public Dictionary<string, decimal> Indicators { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public List<Signal> Signals { get; set; } = new();
    public Dictionary<string, decimal> RiskMetrics { get; set; } = new();
    public Guid? ParentTradeId { get; set; }

    // Properties needed by BacktestResultExtensions
    public DateTime Timestamp => OpenTime;
    public decimal Value => EntryPrice * Quantity;
}
