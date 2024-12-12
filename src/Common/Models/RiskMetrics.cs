namespace TradingSystem.Common.Models;

public class RiskMetrics
{
    public decimal MaxDrawdown { get; set; }
    public decimal SharpeRatio { get; set; }
    public decimal WinRate { get; set; }
    public decimal ProfitFactor { get; set; }
    public decimal ExpectedValue { get; set; }
    public decimal AverageWin { get; set; }
    public decimal AverageLoss { get; set; }
    public decimal MaxConsecutiveLosses { get; set; }
    public decimal ValueAtRisk { get; set; }
    public decimal PortfolioHeatmap { get; set; }
    public Dictionary<string, decimal> PositionSizes { get; set; } = new();
    public Dictionary<string, decimal> RiskAllocation { get; set; } = new();
}
