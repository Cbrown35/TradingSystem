using TradingSystem.Common.Models;

namespace TradingSystem.Common.Interfaces;

public interface IRiskManager
{
    Task<RiskMetrics> GetCurrentRiskMetricsAsync();
    Task<bool> ValidateOrderAsync(Order order);
    Task<bool> ValidatePositionSizeAsync(string symbol, decimal size);
    Task<bool> ValidateDrawdownAsync(decimal drawdown);
    Task<decimal> CalculatePositionSizeAsync(string symbol, decimal riskPercentage);
    Task<decimal> CalculateStopLossAsync(string symbol, decimal entryPrice, decimal riskAmount);
    Task<decimal> CalculateTakeProfitAsync(string symbol, decimal entryPrice, decimal stopLoss);
    Task UpdateRiskParametersAsync(Dictionary<string, decimal> parameters);
    Task<IEnumerable<RiskMetrics>> GetHistoricalRiskMetricsAsync(DateTime startTime, DateTime endTime);
    Task<Dictionary<string, decimal>> GetRiskLimitsAsync();
    Task<bool> IsWithinRiskLimitsAsync(Order order);
}
