namespace TradingSystem.Common.Models;

public class LiquidityMetrics
{
    public string Symbol { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public decimal BidAskSpread { get; set; }
    public decimal MarketDepth { get; set; }
    public decimal TradingVolume { get; set; }
    public decimal OrderBookImbalance { get; set; }
    public decimal AverageTradeSize { get; set; }
    public decimal VolumeWeightedSpread { get; set; }
    public decimal MarketImpact { get; set; }
    public decimal ResiliencyScore { get; set; }
    public Dictionary<decimal, decimal> OrderBookLevels { get; set; } = new();
    public Dictionary<TimeSpan, decimal> IntraMinuteVolume { get; set; } = new();
    public Dictionary<decimal, int> TradeSizeDistribution { get; set; } = new();
}
