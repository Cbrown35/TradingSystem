using TradingSystem.Common.Interfaces;
using TradingSystem.Common.Models;
using TradingSystem.StrategySearch.Services.Backtesting;

namespace TradingSystem.StrategySearch.Services;

public class StrategySearchService
{
    private readonly ITheoryGenerator _theoryGenerator;
    private readonly TheoryBacktester _backtester;
    private readonly IStrategyOptimizer _optimizer;
    private readonly IRiskManager _riskManager;
    private readonly string[] _symbols;

    public StrategySearchService(
        ITheoryGenerator theoryGenerator,
        IMarketDataService marketDataService,
        IStrategyOptimizer optimizer,
        IRiskManager riskManager)
    {
        _theoryGenerator = theoryGenerator;
        _backtester = new TheoryBacktester(marketDataService, riskManager);
        _optimizer = optimizer;
        _riskManager = riskManager;
        
        // Predefined list of symbols to generate theories for
        _symbols = new[] { "BTCUSD", "ETHUSD", "LTCUSD" };
    }

    public async Task<List<OptimizationResult>> SearchStrategies(
        DateTime startDate,
        DateTime endDate,
        int numberOfTheories = 10)
    {
        var results = new List<OptimizationResult>();
        var startTime = DateTime.UtcNow;

        for (int i = 0; i < numberOfTheories; i++)
        {
            // Generate new theory with the predefined symbols
            var theory = await _theoryGenerator.GenerateTheory(_symbols);

            // Initial backtest
            var initialBacktest = await _backtester.RunBacktest(theory, startDate, endDate);

            // Only optimize if the initial results show promise
            if (IsTheoryPromising(initialBacktest))
            {
                // Optimize the theory
                var optimizationResult = await _optimizer.Optimize(theory, startDate, endDate);
                
                // Update risk parameters based on the optimized theory's performance
                if (optimizationResult.BacktestResult.Trades.Any())
                {
                    var riskParameters = ExtractRiskParameters(optimizationResult.BacktestResult);
                    await _riskManager.UpdateRiskParametersAsync(riskParameters);
                }

                // Add timing information
                optimizationResult.OptimizationTime = DateTime.UtcNow - startTime;
                optimizationResult.InitialFitness = CalculateFitness(initialBacktest);
                optimizationResult.FinalFitness = CalculateFitness(optimizationResult.BacktestResult);
                optimizationResult.ImprovementPercentage = 
                    (optimizationResult.FinalFitness - optimizationResult.InitialFitness) / 
                    Math.Abs(optimizationResult.InitialFitness) * 100;

                results.Add(optimizationResult);
            }

            startTime = DateTime.UtcNow;
        }

        // Sort results by final fitness
        return results.OrderByDescending(r => CalculateFitness(r.BacktestResult)).ToList();
    }

    private bool IsTheoryPromising(BacktestResult result)
    {
        // Basic criteria for a promising theory
        return result.SharpeRatio > 0.5m &&
               result.MaxDrawdown < 0.2m &&
               result.ProfitFactor > 1.2m &&
               result.WinRate > 0.4m;
    }

    private decimal CalculateFitness(BacktestResult result)
    {
        // Multi-objective fitness function
        var returnComponent = (result.FinalEquity - 10000) / 10000; // Normalized returns
        var drawdownPenalty = result.MaxDrawdown * 2; // Penalize large drawdowns
        var consistencyBonus = result.SharpeRatio / 2; // Reward consistency
        var profitFactorBonus = Math.Min(result.ProfitFactor, 3) / 3; // Reward good profit factor, capped at 3
        var winRateBonus = result.WinRate - 0.5m; // Reward win rates above 50%

        return returnComponent - drawdownPenalty + consistencyBonus + profitFactorBonus + winRateBonus;
    }

    private Dictionary<string, decimal> ExtractRiskParameters(BacktestResult result)
    {
        var winningTrades = result.Trades.Where(t => (t.RealizedPnL ?? 0) > 0).ToList();
        var losingTrades = result.Trades.Where(t => (t.RealizedPnL ?? 0) <= 0).ToList();

        // Calculate average position sizes and risk metrics from successful trades
        var avgWinningSize = winningTrades.Any() ? winningTrades.Average(t => t.Quantity) : 0m;

        // Calculate average stop loss percentage, handling nullable values
        var avgStopLoss = losingTrades.Any() 
            ? losingTrades
                .Where(t => t.StopLoss.HasValue)
                .Average(t => Math.Abs((t.StopLoss!.Value - t.EntryPrice) / t.EntryPrice)) 
            : 0.02m;

        // Calculate average take profit percentage, handling nullable values
        var avgTakeProfit = winningTrades.Any() 
            ? winningTrades
                .Where(t => t.TakeProfit.HasValue)
                .Average(t => Math.Abs((t.TakeProfit!.Value - t.EntryPrice) / t.EntryPrice)) 
            : 0.04m;

        return new Dictionary<string, decimal>
        {
            ["MaxPositionSize"] = avgWinningSize,
            ["StopLossPercent"] = avgStopLoss * 100, // Convert to percentage
            ["TakeProfitPercent"] = avgTakeProfit * 100, // Convert to percentage
            ["MaxDrawdown"] = result.MaxDrawdown * 100 // Convert to percentage
        };
    }
}
