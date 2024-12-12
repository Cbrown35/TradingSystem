using TradingSystem.Common.Models;

namespace TradingSystem.Common.Interfaces;

public interface ITradingService
{
    Task ExecuteStrategy(Theory strategy, MarketData marketData, decimal positionSize, decimal stopLoss, decimal takeProfit);
    Task<List<Trade>> GetOpenPositions();
    Task CloseAllPositions(string symbol);
    Task<Trade> ClosePosition(string symbol, decimal quantity);
}
