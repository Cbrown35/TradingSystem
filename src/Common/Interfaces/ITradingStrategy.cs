using TradingSystem.Common.Models;

namespace TradingSystem.Common.Interfaces;

public interface ITradingStrategy
{
    string Name { get; }
    Task<bool?> ShouldEnter(List<MarketData> marketData);
    Task<bool?> ShouldExit(List<MarketData> marketData);
    Task<Dictionary<string, decimal>> GetParameters();
    Task SetParameters(Dictionary<string, decimal> parameters);
}
