using TradingSystem.Common.Interfaces;
using TradingSystem.Common.Models;

namespace TradingSystem.Core.Services;

public class TradingSystemCoordinator
{
    private readonly ITradingService _tradingService;
    private readonly IMarketDataService _marketDataService;
    private readonly ITradeRepository _tradeRepository;
    private readonly IRiskManager _riskManager;
    private readonly ITradingStrategy _tradingStrategy;

    public TradingSystemCoordinator(
        ITradingService tradingService,
        IMarketDataService marketDataService,
        ITradeRepository tradeRepository,
        IRiskManager riskManager,
        ITradingStrategy tradingStrategy)
    {
        _tradingService = tradingService;
        _marketDataService = marketDataService;
        _tradeRepository = tradeRepository;
        _riskManager = riskManager;
        _tradingStrategy = tradingStrategy;
    }

    public async Task ExecuteTrading(Theory strategy)
    {
        var riskMetrics = await _riskManager.GetRiskMetrics();
        var openPositions = await _tradeRepository.GetOpenPositions();

        // Monitor existing positions
        foreach (var position in openPositions)
        {
            var marketData = await _marketDataService.GetMarketData(position.Symbol);
            await _tradingService.ExecuteStrategy(strategy, marketData, position.Quantity, position.StopLoss, position.TakeProfit);
        }

        // Look for new opportunities
        var watchlist = strategy.Symbols;
        foreach (var symbol in watchlist)
        {
            if (!openPositions.Any(p => p.Symbol == symbol))
            {
                var marketData = await _marketDataService.GetMarketData(symbol);
                var positionSize = await _riskManager.CalculatePositionSize(symbol, marketData.Close);
                var stopLoss = await _riskManager.CalculateStopLoss(symbol, marketData.Close, true);
                var takeProfit = await _riskManager.CalculateTakeProfit(symbol, marketData.Close, true);

                if (positionSize > 0)
                {
                    await _tradingService.ExecuteStrategy(strategy, marketData, positionSize, stopLoss, takeProfit);
                }
            }
        }
    }

    public async Task<RiskMetrics> GetSystemMetrics()
    {
        return await _riskManager.GetRiskMetrics();
    }
}
