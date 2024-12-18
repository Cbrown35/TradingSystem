using TradingSystem.Common.Interfaces;
using TradingSystem.Common.Models;
using TradingSystem.StrategySearch.Services.Backtesting;

namespace TradingSystem.StrategySearch.Services;

public class Backtester : IBacktester
{
    private readonly IMarketDataService _marketDataService;
    private readonly IRiskManager _riskManager;
    private readonly TradeExecutor _tradeExecutor;
    private readonly BacktestMetricsCalculator _metricsCalculator;
    private readonly ParameterOptimizer _parameterOptimizer;
    private readonly BacktestValidator _validator;

    public Backtester(
        IMarketDataService marketDataService,
        IRiskManager riskManager)
    {
        _marketDataService = marketDataService;
        _riskManager = riskManager;
        _tradeExecutor = new TradeExecutor(riskManager);
        _metricsCalculator = new BacktestMetricsCalculator();
        _parameterOptimizer = new ParameterOptimizer();
        _validator = new BacktestValidator();
    }

    public async Task<BacktestResult> RunBacktestAsync(ITradingStrategy strategy, string symbol, DateTime startTime, DateTime endTime)
    {
        return await RunBacktestAsync(strategy, symbol, startTime, endTime, new Dictionary<string, decimal>());
    }

    public async Task<BacktestResult> RunBacktestAsync(ITradingStrategy strategy, string symbol, DateTime startTime, DateTime endTime, Dictionary<string, decimal> parameters)
    {
        var result = new BacktestResult
        {
            StartDate = startTime,
            EndDate = endTime,
            SymbolPerformance = new Dictionary<string, decimal>()
        };

        decimal initialEquity = parameters.GetValueOrDefault("InitialEquity", 10000m);
        var historicalData = (await _marketDataService.GetHistoricalDataAsync(symbol, startTime, endTime, TimeFrame.Day)).ToList();
        result.SymbolPerformance[symbol] = 0m;

        var finalEquity = await _tradeExecutor.ExecuteTrades(result, symbol, historicalData, strategy, initialEquity);
        _metricsCalculator.CalculateMetrics(result, finalEquity);

        return result;
    }

    public async Task<IEnumerable<BacktestResult>> RunParallelBacktestsAsync(
        ITradingStrategy strategy, 
        string symbol, 
        DateTime startTime, 
        DateTime endTime, 
        IEnumerable<Dictionary<string, decimal>> parameterSets)
    {
        var tasks = parameterSets.Select(parameters => 
            RunBacktestAsync(strategy, symbol, startTime, endTime, parameters));
        
        return await Task.WhenAll(tasks);
    }

    public async Task<BacktestResult> ValidateStrategyAsync(ITradingStrategy strategy, string symbol, DateTime startTime, DateTime endTime)
    {
        var result = await RunBacktestAsync(strategy, symbol, startTime, endTime);
        var (isValid, metrics, messages) = _validator.ValidateBacktestResult(result);

        // Store validation metrics in the SymbolPerformance dictionary
        foreach (var (key, value) in metrics)
        {
            result.SymbolPerformance[$"Validation_{key}"] = value;
        }

        // Store validation messages as metadata in SymbolPerformance with prefix
        foreach (var (key, message) in messages)
        {
            result.SymbolPerformance[$"ValidationMessage_{key}"] = 0; // Use 0 as placeholder since we need decimal
        }

        return result;
    }

    public async Task<Dictionary<string, decimal>> OptimizeParametersAsync(
        ITradingStrategy strategy,
        string symbol,
        DateTime startTime,
        DateTime endTime,
        Dictionary<string, (decimal min, decimal max, decimal step)> parameterRanges)
    {
        var parameterSets = _parameterOptimizer.GenerateParameterSets(parameterRanges);
        var results = await RunParallelBacktestsAsync(strategy, symbol, startTime, endTime, parameterSets);
        
        // Find the parameter set with the best Sharpe ratio
        var bestResult = results.OrderByDescending(r => r.SharpeRatio).First();
        
        // Return the parameters that produced the best result
        return parameterSets.ElementAt(results.ToList().IndexOf(bestResult));
    }
}
