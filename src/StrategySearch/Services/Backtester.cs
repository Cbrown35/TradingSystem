using TradingSystem.Common.Interfaces;
using TradingSystem.Common.Models;

namespace TradingSystem.StrategySearch.Services;

public class Backtester : IBacktester
{
    private readonly IMarketDataService _marketDataService;
    private readonly ITradingStrategy _tradingStrategy;
    private readonly IRiskManager _riskManager;
    private const decimal RiskFreeRate = 0.02m; // 2% annual risk-free rate

    public Backtester(
        IMarketDataService marketDataService,
        ITradingStrategy tradingStrategy,
        IRiskManager riskManager)
    {
        _marketDataService = marketDataService;
        _tradingStrategy = tradingStrategy;
        _riskManager = riskManager;
    }

    public async Task<BacktestResult> RunBacktest(Theory theory, DateTime startDate, DateTime endDate)
    {
        var result = new BacktestResult
        {
            StartDate = startDate,
            EndDate = endDate
        };

        decimal equity = 10000m; // Starting with 10k
        var openPosition = false;
        Trade? currentTrade = null;

        foreach (var symbol in theory.Symbols)
        {
            var historicalData = await _marketDataService.GetHistoricalData(symbol, startDate, endDate);
            result.SymbolPerformance[symbol] = 0m;
            
            for (int i = 50; i < historicalData.Count; i++) // Skip first 50 candles for indicators
            {
                var currentData = historicalData.Take(i + 1).ToList();
                var currentPrice = currentData.Last().Close;

                if (!openPosition)
                {
                    var shouldEnter = await _tradingStrategy.ShouldEnter(currentData);
                    if (shouldEnter == true)
                    {
                        var positionSize = await _riskManager.CalculatePositionSize(symbol, currentPrice);
                        var stopLoss = await _riskManager.CalculateStopLoss(symbol, currentPrice, true);
                        var takeProfit = await _riskManager.CalculateTakeProfit(symbol, currentPrice, true);

                        currentTrade = new Trade
                        {
                            Id = Guid.NewGuid(),
                            Symbol = symbol,
                            EntryPrice = currentPrice,
                            Quantity = positionSize,
                            OpenTime = currentData.Last().Timestamp,
                            Status = TradeStatus.Open,
                            StopLoss = stopLoss,
                            TakeProfit = takeProfit
                        };

                        equity -= (decimal)(positionSize * currentPrice);
                        openPosition = true;
                    }
                }
                else if (currentTrade != null)
                {
                    var shouldExit = await _tradingStrategy.ShouldExit(currentData);
                    var hitStopLoss = currentPrice <= currentTrade.StopLoss;
                    var hitTakeProfit = currentPrice >= currentTrade.TakeProfit;

                    if (shouldExit == true || hitStopLoss || hitTakeProfit)
                    {
                        currentTrade.Status = TradeStatus.Closed;
                        currentTrade.CloseTime = currentData.Last().Timestamp;
                        currentTrade.ExitPrice = currentPrice;
                        currentTrade.RealizedPnL = (currentPrice - currentTrade.EntryPrice) * currentTrade.Quantity;

                        equity += (decimal)(currentTrade.Quantity * currentPrice) + currentTrade.RealizedPnL;
                        result.Trades.Add(currentTrade);
                        result.SymbolPerformance[symbol] += currentTrade.RealizedPnL;

                        openPosition = false;
                        currentTrade = null;
                    }
                }
            }
        }

        // Calculate basic metrics
        result.FinalEquity = equity;
        result.TotalTrades = result.Trades.Count;
        result.WinningTrades = result.Trades.Count(t => t.RealizedPnL > 0);
        result.LosingTrades = result.Trades.Count(t => t.RealizedPnL <= 0);
        result.WinRate = result.TotalTrades > 0 ? (decimal)result.WinningTrades / result.TotalTrades : 0;
        result.ProfitFactor = CalculateProfitFactor(result.Trades);
        result.MaxDrawdown = CalculateMaxDrawdown(result.Trades);

        // Calculate advanced metrics
        var winningTrades = result.Trades.Where(t => t.RealizedPnL > 0).ToList();
        var losingTrades = result.Trades.Where(t => t.RealizedPnL <= 0).ToList();

        result.AverageWin = winningTrades.Any() ? winningTrades.Average(t => t.RealizedPnL) : 0;
        result.AverageLoss = losingTrades.Any() ? Math.Abs(losingTrades.Average(t => t.RealizedPnL)) : 0;
        result.LargestWin = winningTrades.Any() ? winningTrades.Max(t => t.RealizedPnL) : 0;
        result.LargestLoss = losingTrades.Any() ? Math.Abs(losingTrades.Min(t => t.RealizedPnL)) : 0;

        result.MaxConsecutiveWins = CalculateMaxConsecutive(result.Trades, true);
        result.MaxConsecutiveLosses = CalculateMaxConsecutive(result.Trades, false);

        result.AverageHoldingPeriod = result.Trades.Any() 
            ? result.Trades.Average(t => (t.CloseTime!.Value - t.OpenTime).TotalDays) 
            : 0;

        // Calculate risk-adjusted returns
        result.SharpeRatio = CalculateSharpeRatio(result.Trades);
        result.SortinoRatio = CalculateSortinoRatio(result.Trades);

        return result;
    }

    public async Task<BacktestResult> BacktestWithParameters(Theory theory, Dictionary<string, decimal> parameters, string symbol, DateTime startDate, DateTime endDate)
    {
        var result = new BacktestResult
        {
            StartDate = startDate,
            EndDate = endDate
        };

        decimal equity = parameters["InitialEquity"];
        var openPosition = false;
        Trade? currentTrade = null;

        var historicalData = await _marketDataService.GetHistoricalData(symbol, startDate, endDate);
        result.SymbolPerformance[symbol] = 0m;
        
        for (int i = 50; i < historicalData.Count; i++) // Skip first 50 candles for indicators
        {
            var currentData = historicalData.Take(i + 1).ToList();
            var currentPrice = currentData.Last().Close;

            if (!openPosition)
            {
                var shouldEnter = await _tradingStrategy.ShouldEnter(currentData);
                if (shouldEnter == true)
                {
                    var positionSize = await _riskManager.CalculatePositionSize(symbol, currentPrice, parameters["RiskPerTrade"]);
                    var stopLoss = await _riskManager.CalculateStopLoss(symbol, currentPrice, parameters["StopLossMultiplier"]);
                    var takeProfit = await _riskManager.CalculateTakeProfit(symbol, currentPrice, parameters["TakeProfitMultiplier"]);

                    currentTrade = new Trade
                    {
                        Id = Guid.NewGuid(),
                        Symbol = symbol,
                        EntryPrice = currentPrice,
                        Quantity = positionSize,
                        OpenTime = currentData.Last().Timestamp,
                        Status = TradeStatus.Open,
                        StopLoss = stopLoss,
                        TakeProfit = takeProfit
                    };

                    equity -= (decimal)(positionSize * currentPrice);
                    openPosition = true;
                }
            }
            else if (currentTrade != null)
            {
                var shouldExit = await _tradingStrategy.ShouldExit(currentData);
                var hitStopLoss = currentPrice <= currentTrade.StopLoss;
                var hitTakeProfit = currentPrice >= currentTrade.TakeProfit;

                if (shouldExit == true || hitStopLoss || hitTakeProfit)
                {
                    currentTrade.Status = TradeStatus.Closed;
                    currentTrade.CloseTime = currentData.Last().Timestamp;
                    currentTrade.ExitPrice = currentPrice;
                    currentTrade.RealizedPnL = (currentPrice - currentTrade.EntryPrice) * currentTrade.Quantity;

                    equity += (decimal)(currentTrade.Quantity * currentPrice) + currentTrade.RealizedPnL;
                    result.Trades.Add(currentTrade);
                    result.SymbolPerformance[symbol] += currentTrade.RealizedPnL;

                    openPosition = false;
                    currentTrade = null;
                }
            }
        }

        // Calculate basic metrics
        result.FinalEquity = equity;
        result.TotalTrades = result.Trades.Count;
        result.WinningTrades = result.Trades.Count(t => t.RealizedPnL > 0);
        result.LosingTrades = result.Trades.Count(t => t.RealizedPnL <= 0);
        result.WinRate = result.TotalTrades > 0 ? (decimal)result.WinningTrades / result.TotalTrades : 0;
        result.ProfitFactor = CalculateProfitFactor(result.Trades);
        result.MaxDrawdown = CalculateMaxDrawdown(result.Trades);

        // Calculate advanced metrics
        var winningTrades = result.Trades.Where(t => t.RealizedPnL > 0).ToList();
        var losingTrades = result.Trades.Where(t => t.RealizedPnL <= 0).ToList();

        result.AverageWin = winningTrades.Any() ? winningTrades.Average(t => t.RealizedPnL) : 0;
        result.AverageLoss = losingTrades.Any() ? Math.Abs(losingTrades.Average(t => t.RealizedPnL)) : 0;
        result.LargestWin = winningTrades.Any() ? winningTrades.Max(t => t.RealizedPnL) : 0;
        result.LargestLoss = losingTrades.Any() ? Math.Abs(losingTrades.Min(t => t.RealizedPnL)) : 0;

        result.MaxConsecutiveWins = CalculateMaxConsecutive(result.Trades, true);
        result.MaxConsecutiveLosses = CalculateMaxConsecutive(result.Trades, false);

        result.AverageHoldingPeriod = result.Trades.Any() 
            ? result.Trades.Average(t => (t.CloseTime!.Value - t.OpenTime).TotalDays) 
            : 0;

        // Calculate risk-adjusted returns
        result.SharpeRatio = CalculateSharpeRatio(result.Trades);
        result.SortinoRatio = CalculateSortinoRatio(result.Trades);

        return result;
    }

    private decimal CalculateProfitFactor(List<Trade> trades)
    {
        var grossProfit = trades.Where(t => t.RealizedPnL > 0).Sum(t => t.RealizedPnL);
        var grossLoss = Math.Abs(trades.Where(t => t.RealizedPnL <= 0).Sum(t => t.RealizedPnL));

        return grossLoss > 0 ? grossProfit / grossLoss : 0;
    }

    private decimal CalculateMaxDrawdown(List<Trade> trades)
    {
        decimal peak = 0;
        decimal maxDrawdown = 0;
        decimal currentEquity = 10000m;

        foreach (var trade in trades.OrderBy(t => t.OpenTime))
        {
            currentEquity += trade.RealizedPnL;
            
            if (currentEquity > peak)
            {
                peak = currentEquity;
            }

            var drawdown = (peak - currentEquity) / peak;
            maxDrawdown = Math.Max(maxDrawdown, drawdown);
        }

        return maxDrawdown;
    }

    private int CalculateMaxConsecutive(List<Trade> trades, bool winning)
    {
        int maxConsecutive = 0;
        int currentConsecutive = 0;

        foreach (var trade in trades.OrderBy(t => t.OpenTime))
        {
            if ((winning && trade.RealizedPnL > 0) || (!winning && trade.RealizedPnL <= 0))
            {
                currentConsecutive++;
                maxConsecutive = Math.Max(maxConsecutive, currentConsecutive);
            }
            else
            {
                currentConsecutive = 0;
            }
        }

        return maxConsecutive;
    }

    private decimal CalculateSharpeRatio(List<Trade> trades)
