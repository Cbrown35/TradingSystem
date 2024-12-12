using TradingSystem.Common.Models;

namespace TradingSystem.Common.Interfaces;

public interface IRiskManager
{
    Task<RiskMetrics> GetRiskMetrics();
    Task<RiskMetrics> UpdateRiskMetrics(Trade trade);
    Task<decimal> CalculatePositionSize(string symbol, decimal price);
    Task<decimal> CalculateStopLoss(string symbol, decimal entryPrice, bool isLong);
    Task<decimal> CalculateTakeProfit(string symbol, decimal entryPrice, bool isLong);
    Task<bool> ValidateRiskParameters(string symbol, decimal positionSize, decimal stopLoss, decimal takeProfit);
    Task<Dictionary<string, decimal>> GetPortfolioAllocation();
    Task<Dictionary<string, decimal>> GetRiskExposure();
}
