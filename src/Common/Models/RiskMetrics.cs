using System.Collections.Generic;

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
    public decimal AccountEquity { get; set; }
    public decimal MarginUsed { get; set; }
    public decimal TotalRisk { get; set; }
    public int TotalTrades { get; set; }
    public List<decimal> EquityHistory { get; set; } = new();
    public List<Trade> OpenPositions { get; set; } = new();
    public Dictionary<string, decimal> PositionSizes { get; set; } = new();
    public Dictionary<string, decimal> RiskAllocation { get; set; } = new();
    public Dictionary<string, decimal> RiskParameters { get; set; } = new();
}
