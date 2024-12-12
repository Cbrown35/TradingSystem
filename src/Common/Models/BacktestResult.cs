namespace TradingSystem.Common.Models;

public class BacktestResult
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<Trade> Trades { get; set; } = new();
    public decimal FinalEquity { get; set; }
    public int TotalTrades { get; set; }
    public int WinningTrades { get; set; }
    public int LosingTrades { get; set; }
    public decimal WinRate { get; set; }
    public decimal ProfitFactor { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal SharpeRatio { get; set; }
    public decimal SortinoRatio { get; set; }
    public decimal AverageWin { get; set; }
    public decimal AverageLoss { get; set; }
    public decimal LargestWin { get; set; }
    public decimal LargestLoss { get; set; }
    public int MaxConsecutiveWins { get; set; }
    public int MaxConsecutiveLosses { get; set; }
    public decimal AverageHoldingPeriod { get; set; }
    public Dictionary<string, decimal> SymbolPerformance { get; set; } = new();
}
