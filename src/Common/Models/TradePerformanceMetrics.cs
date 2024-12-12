namespace TradingSystem.Common.Models;

public class TradePerformanceMetrics
{
    public int TotalTrades { get; set; }
    public int WinningTrades { get; set; }
    public int LosingTrades { get; set; }
    public decimal WinRate { get; set; }
    public decimal AverageWin { get; set; }
    public decimal AverageLoss { get; set; }
    public decimal LargestWin { get; set; }
    public decimal LargestLoss { get; set; }
    public decimal TotalPnL { get; set; }
    public decimal ProfitFactor { get; set; }
    public TimeSpan AverageHoldingTime { get; set; }
    public Dictionary<string, decimal> SymbolPerformance { get; set; } = new();
    public Dictionary<string, decimal> StrategyPerformance { get; set; } = new();
    public decimal SharpeRatio { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal ReturnOnInvestment { get; set; }
    public int MaxConsecutiveWins { get; set; }
    public int MaxConsecutiveLosses { get; set; }
    public decimal ExpectedValue { get; set; }
    public Dictionary<string, decimal> MonthlyReturns { get; set; } = new();
    public Dictionary<TimeSpan, decimal> HoldingTimeDistribution { get; set; } = new();
    public Dictionary<decimal, int> ProfitDistribution { get; set; } = new();
    public Dictionary<DayOfWeek, decimal> DayOfWeekPerformance { get; set; } = new();
    public Dictionary<int, decimal> HourOfDayPerformance { get; set; } = new();
}
