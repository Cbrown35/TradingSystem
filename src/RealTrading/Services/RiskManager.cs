using TradingSystem.Common.Interfaces;
using TradingSystem.Common.Models;

namespace TradingSystem.RealTrading.Services;

public class RiskManager : IRiskManager
{
    private readonly Dictionary<string, RiskMetrics> _riskMetrics;
    private readonly decimal _maxRiskPerTrade = 0.02m; // 2% max risk per trade
    private readonly decimal _maxPortfolioRisk = 0.06m; // 6% max portfolio risk
    private readonly decimal _defaultStopLossPercent = 0.02m; // 2% default stop loss
    private readonly decimal _defaultTakeProfitPercent = 0.04m; // 4% default take profit

    public RiskManager()
    {
        _riskMetrics = new Dictionary<string, RiskMetrics>();
    }

    public async Task<RiskMetrics> GetRiskMetrics()
    {
        var combinedMetrics = new RiskMetrics();
        foreach (var metrics in _riskMetrics.Values)
        {
            combinedMetrics.MaxDrawdown = Math.Max(combinedMetrics.MaxDrawdown, metrics.MaxDrawdown);
            combinedMetrics.SharpeRatio = (combinedMetrics.SharpeRatio + metrics.SharpeRatio) / 2;
            combinedMetrics.WinRate = (combinedMetrics.WinRate + metrics.WinRate) / 2;
            combinedMetrics.ProfitFactor = (combinedMetrics.ProfitFactor + metrics.ProfitFactor) / 2;
            combinedMetrics.ExpectedValue = (combinedMetrics.ExpectedValue + metrics.ExpectedValue) / 2;
        }
        return combinedMetrics;
    }

    public async Task<RiskMetrics> UpdateRiskMetrics(Trade trade)
    {
        if (!_riskMetrics.ContainsKey(trade.Symbol))
        {
            _riskMetrics[trade.Symbol] = new RiskMetrics();
        }

        var metrics = _riskMetrics[trade.Symbol];

        // Update metrics based on trade result
        if (trade.Status == TradeStatus.Closed)
        {
            var profitLoss = trade.RealizedPnL;
            var riskAmount = trade.Quantity * Math.Abs(trade.EntryPrice - trade.StopLoss);

            // Update win rate
            if (profitLoss > 0)
            {
                metrics.WinRate = (metrics.WinRate * 0.9m) + (1 * 0.1m); // Exponential moving average
                metrics.AverageWin = (metrics.AverageWin * 0.9m) + (profitLoss * 0.1m);
            }
            else
            {
                metrics.WinRate = metrics.WinRate * 0.9m; // Decay win rate
                metrics.AverageLoss = (metrics.AverageLoss * 0.9m) + (Math.Abs(profitLoss) * 0.1m);
            }

            // Update profit factor
            if (metrics.AverageLoss != 0)
            {
                metrics.ProfitFactor = metrics.AverageWin / metrics.AverageLoss;
            }

            // Update expected value
            metrics.ExpectedValue = (metrics.WinRate * metrics.AverageWin) - ((1 - metrics.WinRate) * metrics.AverageLoss);

            // Update value at risk
            metrics.ValueAtRisk = Math.Max(metrics.ValueAtRisk, riskAmount);
        }

        return metrics;
    }

    public async Task<decimal> CalculatePositionSize(string symbol, decimal price)
    {
        var metrics = await GetRiskMetrics();
        var currentRisk = metrics.PortfolioHeatmap;

        // If we're approaching max portfolio risk, reduce position size
        if (currentRisk >= _maxPortfolioRisk)
        {
            return 0; // No new positions
        }

        // Calculate position size based on available risk
        var availableRisk = _maxPortfolioRisk - currentRisk;
        var riskAmount = Math.Min(availableRisk, _maxRiskPerTrade);
        
        return (riskAmount * price) / _defaultStopLossPercent;
    }

    public async Task<decimal> CalculateStopLoss(string symbol, decimal entryPrice, bool isLong)
    {
        var stopLossAmount = entryPrice * _defaultStopLossPercent;
        return isLong ? entryPrice - stopLossAmount : entryPrice + stopLossAmount;
    }

    public async Task<decimal> CalculateTakeProfit(string symbol, decimal entryPrice, bool isLong)
    {
        var takeProfitAmount = entryPrice * _defaultTakeProfitPercent;
        return isLong ? entryPrice + takeProfitAmount : entryPrice - takeProfitAmount;
    }

    public async Task<bool> ValidateRiskParameters(string symbol, decimal positionSize, decimal stopLoss, decimal takeProfit)
    {
        var metrics = await GetRiskMetrics();
        
        // Check if total portfolio risk is acceptable
        if (metrics.PortfolioHeatmap >= _maxPortfolioRisk)
        {
            return false;
        }

        // Calculate risk amount for this trade
        var riskAmount = positionSize * Math.Abs(takeProfit - stopLoss);
        var riskPercent = riskAmount / metrics.PortfolioHeatmap;

        // Validate risk per trade
        if (riskPercent > _maxRiskPerTrade)
        {
            return false;
        }

        return true;
    }

    public async Task<Dictionary<string, decimal>> GetPortfolioAllocation()
    {
        var allocation = new Dictionary<string, decimal>();
        var totalRisk = _riskMetrics.Values.Sum(m => m.ValueAtRisk);

        foreach (var kvp in _riskMetrics)
        {
            allocation[kvp.Key] = totalRisk > 0 ? kvp.Value.ValueAtRisk / totalRisk : 0;
        }

        return allocation;
    }

    public async Task<Dictionary<string, decimal>> GetRiskExposure()
    {
        var exposure = new Dictionary<string, decimal>();
        foreach (var kvp in _riskMetrics)
        {
            exposure[kvp.Key] = kvp.Value.ValueAtRisk;
        }
        return exposure;
    }
}
