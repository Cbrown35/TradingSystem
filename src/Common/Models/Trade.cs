namespace TradingSystem.Common.Models;

public class Trade
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string StrategyName { get; set; } = string.Empty;
    public decimal EntryPrice { get; set; }
    public decimal? ExitPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal StopLoss { get; set; }
    public decimal TakeProfit { get; set; }
    public decimal RealizedPnL { get; set; }
    public decimal Commission { get; set; }
    public decimal Slippage { get; set; }
    public TradeStatus Status { get; set; }
    public DateTime OpenTime { get; set; }
    public DateTime? CloseTime { get; set; }
    public string Notes { get; set; } = string.Empty;
    public Dictionary<string, decimal> Indicators { get; set; } = new();
    public Dictionary<string, string> Tags { get; set; } = new();
    public TradeDirection Direction { get; set; }
    public OrderType EntryOrderType { get; set; }
    public OrderType ExitOrderType { get; set; }
    public decimal? UnrealizedPnL { get; set; }
    public decimal? DrawdownFromPeak { get; set; }
    public decimal? RiskRewardRatio { get; set; }
    public decimal? ReturnOnInvestment { get; set; }
    public TimeSpan? HoldingTime => CloseTime.HasValue ? CloseTime.Value - OpenTime : null;
    public string MarketCondition { get; set; } = string.Empty;
    public string SetupType { get; set; } = string.Empty;
    public int? ReEntryCount { get; set; }
    public Guid? ParentTradeId { get; set; }
    public List<string> Signals { get; set; } = new();
    public Dictionary<string, decimal> RiskMetrics { get; set; } = new();
}

public enum TradeStatus
{
    Open,
    Closed,
    Cancelled,
    Pending,
    PartiallyFilled,
    Error
}

public enum TradeDirection
{
    Long,
    Short
}
