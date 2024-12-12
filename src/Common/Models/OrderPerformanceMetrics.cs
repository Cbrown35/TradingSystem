namespace TradingSystem.Common.Models;

public class OrderPerformanceMetrics
{
    public int TotalOrders { get; set; }
    public int FilledOrders { get; set; }
    public int PartiallyFilledOrders { get; set; }
    public int CanceledOrders { get; set; }
    public int RejectedOrders { get; set; }
    public decimal AverageSlippage { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal AverageFillRate { get; set; }
    public Dictionary<OrderType, int> OrderTypeDistribution { get; set; } = new();
    public Dictionary<OrderSide, int> OrderSideDistribution { get; set; } = new();
    public double AverageProcessingTime { get; set; }
    public Dictionary<string, int> SymbolDistribution { get; set; } = new();
    public Dictionary<int, int> HourlyDistribution { get; set; } = new();
    public Dictionary<string, decimal> StrategyPerformance { get; set; } = new();
    public Dictionary<string, decimal> VenuePerformance { get; set; } = new();
    public Dictionary<TimeSpan, int> ProcessingTimeDistribution { get; set; } = new();
    public Dictionary<decimal, int> SlippageDistribution { get; set; } = new();
    public Dictionary<decimal, int> CommissionDistribution { get; set; } = new();
    public Dictionary<DayOfWeek, int> DayOfWeekDistribution { get; set; } = new();
    public Dictionary<string, decimal> MarketImpact { get; set; } = new();
    public Dictionary<string, double> VenueFillRates { get; set; } = new();
    public Dictionary<string, decimal> VenueLatency { get; set; } = new();
    public Dictionary<OrderStatus, int> StatusDistribution { get; set; } = new();
    public Dictionary<string, OrderVenueMetrics> VenueMetrics { get; set; } = new();
}

public class OrderVenueMetrics
{
    public int TotalOrders { get; set; }
    public int SuccessfulOrders { get; set; }
    public int FailedOrders { get; set; }
    public decimal AverageLatency { get; set; }
    public decimal AverageSlippage { get; set; }
    public decimal AverageCommission { get; set; }
    public decimal FillRate { get; set; }
    public Dictionary<OrderType, int> SupportedOrderTypes { get; set; } = new();
    public Dictionary<OrderStatus, int> StatusDistribution { get; set; } = new();
    public Dictionary<string, int> ErrorDistribution { get; set; } = new();
    public Dictionary<TimeSpan, int> LatencyDistribution { get; set; } = new();
    public Dictionary<decimal, int> CommissionTiers { get; set; } = new();
    public Dictionary<string, decimal> SymbolLiquidity { get; set; } = new();
    public Dictionary<string, decimal> SymbolSpread { get; set; } = new();
    public Dictionary<int, decimal> HourlyFillRates { get; set; } = new();
    public Dictionary<DayOfWeek, decimal> DailyFillRates { get; set; } = new();
    public Dictionary<string, decimal> MarketImpact { get; set; } = new();
    public Dictionary<decimal, int> OrderSizeDistribution { get; set; } = new();
    public Dictionary<string, decimal> RejectionReasons { get; set; } = new();
}
