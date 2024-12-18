using TradingSystem.Common.Models;

namespace TradingSystem.Common.Interfaces;

public interface IBacktester
{
    Task<BacktestResult> RunBacktestAsync(ITradingStrategy strategy, string symbol, DateTime startTime, DateTime endTime);
    Task<BacktestResult> RunBacktestAsync(ITradingStrategy strategy, string symbol, DateTime startTime, DateTime endTime, Dictionary<string, decimal> parameters);
    Task<IEnumerable<BacktestResult>> RunParallelBacktestsAsync(ITradingStrategy strategy, string symbol, DateTime startTime, DateTime endTime, IEnumerable<Dictionary<string, decimal>> parameterSets);
    Task<BacktestResult> ValidateStrategyAsync(ITradingStrategy strategy, string symbol, DateTime startTime, DateTime endTime);
    Task<Dictionary<string, decimal>> OptimizeParametersAsync(ITradingStrategy strategy, string symbol, DateTime startTime, DateTime endTime, Dictionary<string, (decimal min, decimal max, decimal step)> parameterRanges);
}
