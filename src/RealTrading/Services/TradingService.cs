using TradingSystem.Common.Interfaces;
using TradingSystem.Common.Models;

namespace TradingSystem.RealTrading.Services;

public class TradingService : ITradingService
{
    private readonly ITradeRepository _tradeRepository;
    private readonly ITradingStrategy _tradingStrategy;
    private readonly IMarketDataService _marketDataService;
    private readonly IExchangeAdapter _exchangeAdapter;
    private readonly IRiskManager _riskManager;

    public TradingService(
        ITradeRepository tradeRepository,
        ITradingStrategy tradingStrategy,
        IMarketDataService marketDataService,
        IExchangeAdapter exchangeAdapter,
        IRiskManager riskManager)
    {
        _tradeRepository = tradeRepository;
        _tradingStrategy = tradingStrategy;
        _marketDataService = marketDataService;
        _exchangeAdapter = exchangeAdapter;
        _riskManager = riskManager;
    }

    public async Task ExecuteStrategy(Theory strategy, MarketData marketData, decimal positionSize, decimal stopLoss, decimal takeProfit)
    {
        var historicalData = await _marketDataService.GetHistoricalData(
            marketData.Symbol,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow);

        var shouldEnter = await _tradingStrategy.ShouldEnter(historicalData);
        if (shouldEnter == true)
        {
            await OpenPosition(marketData.Symbol, positionSize, marketData.Close, stopLoss, takeProfit);
        }

        var openPositions = await _tradeRepository.GetOpenPositions();
        foreach (var position in openPositions)
        {
            var shouldExit = await _tradingStrategy.ShouldExit(historicalData);
            if (shouldExit == true)
            {
                await ClosePosition(position.Symbol, position.Quantity);
            }
            else
            {
                // Check stop loss and take profit
                if (marketData.Low <= position.StopLoss || marketData.High >= position.TakeProfit)
                {
                    await ClosePosition(position.Symbol, position.Quantity);
                }
            }
        }
    }

    public async Task<List<Trade>> GetOpenPositions()
    {
        return await _tradeRepository.GetOpenPositions();
    }

    public async Task CloseAllPositions(string symbol)
    {
        var openPositions = await _tradeRepository.GetOpenPositions();
        foreach (var position in openPositions.Where(p => p.Symbol == symbol))
        {
            await ClosePosition(position.Symbol, position.Quantity);
        }
    }

    public async Task<Trade> ClosePosition(string symbol, decimal quantity)
    {
        var openPositions = await _tradeRepository.GetOpenPositions();
        var position = openPositions.FirstOrDefault(p => p.Symbol == symbol);
        if (position == null)
        {
            throw new InvalidOperationException($"No open position found for {symbol}");
        }

        var closedOrder = await _exchangeAdapter.CloseOrder(position.Id.ToString());
        position.Status = TradeStatus.Closed;
        position.CloseTime = DateTime.UtcNow;
        position.RealizedPnL = closedOrder.RealizedPnL;

        await _tradeRepository.UpdateTrade(position);
        await _riskManager.UpdateRiskMetrics(position);

        return position;
    }

    private async Task<Trade> OpenPosition(string symbol, decimal quantity, decimal price, decimal stopLoss, decimal takeProfit)
    {
        var order = await _exchangeAdapter.PlaceOrder(symbol, quantity, price, true, OrderType.Market);
        order.StopLoss = stopLoss;
        order.TakeProfit = takeProfit;

        var trade = await _tradeRepository.AddTrade(order);
        await _riskManager.UpdateRiskMetrics(trade);

        return trade;
    }
}
