namespace TradingSystem.Common.Models;

public class OrderVenueMetrics
{
    public string VenueId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public decimal? LatencyMs { get; set; }
    public decimal? FillRate { get; set; }
    public decimal? PriceImprovement { get; set; }
    public decimal? MarketImpact { get; set; }
    public decimal? SpreadCost { get; set; }
    public decimal? FeeRate { get; set; }
    public decimal? TotalFees { get; set; }
    public decimal? RebateRate { get; set; }
    public decimal? TotalRebates { get; set; }
    public decimal? NetCost { get; set; }
    public decimal? LiquidityScore { get; set; }
    public int TotalOrders { get; set; }
    public int SuccessfulOrders { get; set; }
    public int FailedOrders { get; set; }
    public decimal? AverageLatency { get; set; }
    public decimal? AverageSlippage { get; set; }
    public decimal? AverageCommission { get; set; }
    public List<OrderType> SupportedOrderTypes { get; set; } = new();
    public Dictionary<string, decimal> StatusDistribution { get; set; } = new();
    public Dictionary<string, int> ErrorDistribution { get; set; } = new();
    public Dictionary<decimal, int> LatencyDistribution { get; set; } = new();
    public Dictionary<int, decimal> HourlyFillRates { get; set; } = new();
    public Dictionary<string, decimal> DailyFillRates { get; set; } = new();
    public Dictionary<decimal, int> OrderSizeDistribution { get; set; } = new();
    public Dictionary<decimal, decimal> CommissionTiers { get; set; } = new();
    public Dictionary<string, decimal> SymbolLiquidity { get; set; } = new();
    public Dictionary<string, decimal> SymbolSpread { get; set; } = new();
    public Dictionary<string, decimal> CustomMetrics { get; set; } = new();
}
