using TradingSystem.Common.Models;

namespace TradingSystem.Common.Interfaces;

public interface IBacktester
{
    Task<BacktestResult> RunBacktest(Theory theory, DateTime startDate, DateTime endDate);
    Task<BacktestResult> BacktestWithParameters(Theory theory, Dictionary<string, decimal> parameters, string symbol, DateTime startDate, DateTime endDate);
}
