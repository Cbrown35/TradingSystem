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
    public decimal AverageProcessingTime { get; set; }
    public Dictionary<string, int> SymbolDistribution { get; set; } = new();
    public Dictionary<int, int> HourlyDistribution { get; set; } = new();
    public Dictionary<string, decimal> StrategyPerformance { get; set; } = new();
    public Dictionary<string, decimal> VenuePerformance { get; set; } = new();
    public Dictionary<TimeSpan, int> ProcessingTimeDistribution { get; set; } = new();
    public Dictionary<decimal, int> SlippageDistribution { get; set; } = new();
    public Dictionary<decimal, int> CommissionDistribution { get; set; } = new();
    public Dictionary<DayOfWeek, int> DayOfWeekDistribution { get; set; } = new();
    public Dictionary<string, decimal> MarketImpact { get; set; } = new();
    public Dictionary<string, decimal> VenueFillRates { get; set; } = new();  // Changed from double to decimal
    public Dictionary<string, decimal> VenueLatency { get; set; } = new();
    public Dictionary<OrderStatus, int> StatusDistribution { get; set; } = new();
    public Dictionary<string, OrderVenueMetrics> VenueMetrics { get; set; } = new();
}
