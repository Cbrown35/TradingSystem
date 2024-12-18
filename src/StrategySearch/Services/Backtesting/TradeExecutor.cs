using TradingSystem.Common.Interfaces;
using TradingSystem.Common.Models;

namespace TradingSystem.StrategySearch.Services.Backtesting;

internal class TradeExecutor
{
    private readonly IRiskManager _riskManager;
    private const int MinimumDataPoints = 50;

    public TradeExecutor(IRiskManager riskManager)
    {
        _riskManager = riskManager;
    }

    public async Task<decimal> ExecuteTrades(
        BacktestResult result,
        string symbol,
        IList<MarketData> historicalData,
        ITradingStrategy strategy,
        decimal initialEquity)
    {
        if (historicalData.Count < MinimumDataPoints)
        {
            return initialEquity;
        }

        var equity = initialEquity;
        var openPosition = false;
        Trade? currentTrade = null;

        for (int i = MinimumDataPoints; i < historicalData.Count; i++)
        {
            var currentData = historicalData.Take(i + 1).ToList();
            var currentPrice = currentData.Last().Close;

            if (!openPosition)
            {
                var shouldEnter = await strategy.ShouldEnter(currentData);
                if (shouldEnter == true)
                {
                    currentTrade = await OpenTrade(symbol, currentPrice, currentData.Last().Timestamp);
                    if (currentTrade != null)
                    {
                        equity -= currentTrade.Quantity * currentPrice;
                        openPosition = true;
                    }
                }
            }
            else if (currentTrade != null)
            {
                var shouldExit = await strategy.ShouldExit(currentData);
                var hitStopLoss = currentTrade.StopLoss.HasValue && currentPrice <= currentTrade.StopLoss.Value;
                var hitTakeProfit = currentTrade.TakeProfit.HasValue && currentPrice >= currentTrade.TakeProfit.Value;

                if (shouldExit == true || hitStopLoss || hitTakeProfit)
                {
                    await CloseTrade(currentTrade, currentPrice, currentData.Last().Timestamp);
                    equity += currentTrade.Quantity * currentPrice + (currentTrade.RealizedPnL ?? 0);
                    result.Trades.Add(currentTrade);
                    result.SymbolPerformance[symbol] += currentTrade.RealizedPnL ?? 0;

                    openPosition = false;
                    currentTrade = null;
                }
            }
        }

        return equity;
    }

    private async Task<Trade?> OpenTrade(string symbol, decimal currentPrice, DateTime timestamp)
    {
        var positionSize = await _riskManager.CalculatePositionSizeAsync(symbol, currentPrice);
        var stopLoss = await _riskManager.CalculateStopLossAsync(symbol, currentPrice, currentPrice);
        var takeProfit = await _riskManager.CalculateTakeProfitAsync(symbol, currentPrice, currentPrice);

        return new Trade
        {
            Id = Guid.NewGuid(),
            Symbol = symbol,
            EntryPrice = currentPrice,
            Quantity = positionSize,
            OpenTime = timestamp,
            Status = TradeStatus.Open,
            StopLoss = stopLoss,
            TakeProfit = takeProfit
        };
    }

    private Task CloseTrade(Trade trade, decimal currentPrice, DateTime timestamp)
    {
        trade.Status = TradeStatus.Closed;
        trade.CloseTime = timestamp;
        trade.ExitPrice = currentPrice;
        trade.RealizedPnL = (currentPrice - trade.EntryPrice) * trade.Quantity;
        
        return Task.CompletedTask;
    }
}
