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

    public async Task ExecuteTrading(Theory theory)
    {
        var riskMetrics = await _riskManager.GetCurrentRiskMetricsAsync();
        var openPositions = await _tradeRepository.GetOpenPositions();
        var activeStrategies = await _tradingService.GetActiveStrategiesAsync();

        // Monitor existing positions
        foreach (var position in openPositions)
        {
            var marketData = await _marketDataService.GetLatestMarketDataAsync(position.Symbol);
            if (marketData != null)
            {
                var parameters = new Dictionary<string, decimal>
                {
                    ["SymbolId"] = GetSymbolId(position.Symbol),
                    ["Quantity"] = position.Quantity,
                    ["CurrentPrice"] = marketData.Close
                };

                // Only add StopLoss and TakeProfit if they have values
                if (position.StopLoss.HasValue)
                {
                    parameters["StopLoss"] = position.StopLoss.Value;
                }
                if (position.TakeProfit.HasValue)
                {
                    parameters["TakeProfit"] = position.TakeProfit.Value;
                }

                // Update strategy parameters for the position
                await _tradingService.UpdateStrategyParametersAsync(_tradingStrategy.Name, parameters);
            }
        }

        // Look for new opportunities
        var watchlist = theory.Symbols;
        foreach (var symbol in watchlist)
        {
            if (!openPositions.Any(p => p.Symbol == symbol))
            {
                var marketData = await _marketDataService.GetLatestMarketDataAsync(symbol);
                if (marketData != null)
                {
                    var positionSize = await _riskManager.CalculatePositionSizeAsync(symbol, marketData.Close);
                    var stopLoss = await _riskManager.CalculateStopLossAsync(symbol, marketData.Close, marketData.Close);
                    var takeProfit = await _riskManager.CalculateTakeProfitAsync(symbol, marketData.Close, marketData.Close);

                    if (positionSize > 0)
                    {
                        // Start strategy if not already active
                        if (!activeStrategies.Any(s => s.Name == _tradingStrategy.Name))
                        {
                            await _tradingService.StartStrategyAsync(_tradingStrategy);
                        }

                        // Update trading parameters
                        var parameters = new Dictionary<string, decimal>
                        {
                            ["SymbolId"] = GetSymbolId(symbol),
                            ["Quantity"] = positionSize,
                            ["EntryPrice"] = marketData.Close
                        };

                        // Only add StopLoss and TakeProfit if they have values
                        if (stopLoss > 0)
                        {
                            parameters["StopLoss"] = stopLoss;
                        }
                        if (takeProfit > 0)
                        {
                            parameters["TakeProfit"] = takeProfit;
                        }

                        await _tradingService.UpdateStrategyParametersAsync(_tradingStrategy.Name, parameters);
                    }
                }
            }
        }
    }

    public async Task<RiskMetrics> GetSystemMetrics()
    {
        return await _riskManager.GetCurrentRiskMetricsAsync();
    }

    private decimal GetSymbolId(string symbol)
    {
        // Simple hash function to convert symbol to a decimal ID
        // This is just for parameter passing and doesn't need to persist
        decimal id = 0;
        for (int i = 0; i < symbol.Length; i++)
        {
            id = (id * 31 + symbol[i]) % 1000000;
        }
        return id;
    }
}
