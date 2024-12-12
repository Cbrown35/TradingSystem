using TradingSystem.Common.Models;

namespace TradingSystem.Common.Interfaces;

public interface IStrategyOptimizer
{
    Task<OptimizationResult> Optimize(Theory baseTheory, DateTime startDate, DateTime endDate);
    Task<OptimizationResult> OptimizeParameters(Theory theory, OptimizationSettings settings);
}
