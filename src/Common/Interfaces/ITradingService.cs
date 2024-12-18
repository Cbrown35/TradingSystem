using TradingSystem.Common.Models;

namespace TradingSystem.Common.Interfaces;

public interface ITradingService
{
    Task<IEnumerable<ITradingStrategy>> GetActiveStrategiesAsync();
    Task<StrategyPerformance> GetStrategyPerformanceAsync(string strategyId);
    Task StartStrategyAsync(ITradingStrategy strategy);
    Task StopStrategyAsync(string strategyId);
    Task PauseStrategyAsync(string strategyId);
    Task ResumeStrategyAsync(string strategyId);
    Task UpdateStrategyParametersAsync(string strategyId, Dictionary<string, decimal> parameters);
    Task<IEnumerable<Trade>> GetStrategyTradesAsync(string strategyId, DateTime? startTime = null, DateTime? endTime = null);
    Task<IEnumerable<Order>> GetStrategyOrdersAsync(string strategyId, DateTime? startTime = null, DateTime? endTime = null);
    Task<decimal> GetStrategyPnLAsync(string strategyId, DateTime? startTime = null, DateTime? endTime = null);
    Task<Dictionary<string, decimal>> GetStrategyMetricsAsync(string strategyId);
}

public class StrategyPerformance
{
    public bool IsExecuting { get; set; }
    public decimal ErrorRate { get; set; }
    public TimeSpan AverageLatency { get; set; }
    public int TotalTrades { get; set; }
    public int SuccessfulTrades { get; set; }
    public decimal WinRate { get; set; }
    public decimal ProfitFactor { get; set; }
    public decimal SharpeRatio { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal ReturnOnInvestment { get; set; }
    public Dictionary<string, decimal> CustomMetrics { get; set; } = new();
}
