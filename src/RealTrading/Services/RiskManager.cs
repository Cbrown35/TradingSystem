using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Common.Interfaces;
using TradingSystem.Common.Models;
using TradingSystem.RealTrading.Configuration;

namespace TradingSystem.RealTrading.Services;

public class RiskManager : IRiskManager
{
    private readonly ITradeRepository _tradeRepository;
    private readonly IRiskValidationService _validationService;
    private readonly RiskManagerConfig _config;
    private readonly ILogger<RiskManager> _logger;
    private readonly Dictionary<string, RiskMetrics> _riskMetrics;

    public RiskManager(
        ITradeRepository tradeRepository,
        IRiskValidationService validationService,
        IOptions<RiskManagerConfig> config,
        ILogger<RiskManager> logger)
    {
        _tradeRepository = tradeRepository;
        _validationService = validationService;
        _config = config.Value;
        _logger = logger;
        _riskMetrics = new Dictionary<string, RiskMetrics>();
    }

    public async Task<RiskMetrics> GetCurrentRiskMetricsAsync()
    {
        try
        {
            var metrics = new RiskMetrics
            {
                AccountEquity = await CalculateAccountEquity(),
                OpenPositions = await _tradeRepository.GetOpenPositions()
            };

            // Calculate current risk metrics
            metrics.TotalRisk = metrics.OpenPositions.Sum(p => CalculatePositionRisk(p));
            metrics.MarginUsed = metrics.OpenPositions.Sum(p => p.Quantity * p.EntryPrice);
            metrics.MaxDrawdown = await CalculateMaxDrawdown();

            _logger.LogDebug("Current risk metrics - Equity: {Equity}, Total Risk: {Risk}, Margin Used: {Margin}",
                metrics.AccountEquity, metrics.TotalRisk, metrics.MarginUsed);

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current risk metrics");
            throw;
        }
    }

    public async Task<bool> ValidateOrderAsync(Order order)
    {
        try
        {
            var currentMetrics = await GetCurrentRiskMetricsAsync();
            var result = _validationService.ValidateOrder(order, currentMetrics);

            if (!result.IsValid)
            {
                _logger.LogWarning("Order validation failed: {Errors}",
                    string.Join(", ", result.ValidationErrors.Values));
            }

            return result.IsValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating order");
            throw;
        }
    }

    public async Task<bool> ValidatePositionSizeAsync(string symbol, decimal size)
    {
        try
        {
            var accountEquity = await CalculateAccountEquity();
            var result = _validationService.ValidatePositionSize(symbol, size, accountEquity);

            if (!result.IsValid)
            {
                _logger.LogWarning("Position size validation failed: {Errors}",
                    string.Join(", ", result.ValidationErrors.Values));
            }

            return result.IsValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating position size");
            throw;
        }
    }

    public async Task<bool> ValidateDrawdownAsync(decimal drawdown)
    {
        try
        {
            var result = _validationService.ValidateDrawdown(drawdown);

            if (!result.IsValid)
            {
                _logger.LogWarning("Drawdown validation failed: {Error}", result.ErrorMessage);
            }

            return result.IsValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating drawdown");
            throw;
        }
    }

    public async Task<decimal> CalculatePositionSizeAsync(string symbol, decimal riskPercentage)
    {
        try
        {
            var accountEquity = await CalculateAccountEquity();
            var riskAmount = accountEquity * Math.Min(riskPercentage, _config.MaxRiskPerTrade);
            var positionSize = riskAmount / _config.DefaultStopLossPercent;

            // Validate the calculated position size
            var result = _validationService.ValidatePositionSize(symbol, positionSize, accountEquity);
            if (!result.IsValid)
            {
                _logger.LogWarning("Calculated position size validation failed: {Errors}",
                    string.Join(", ", result.ValidationErrors.Values));
                return 0;
            }

            return positionSize;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating position size");
            throw;
        }
    }

    public async Task<decimal> CalculateStopLossAsync(string symbol, decimal entryPrice, decimal riskAmount)
    {
        try
        {
            var accountEquity = await CalculateAccountEquity();
            var stopLoss = entryPrice - (riskAmount / entryPrice);

            var result = _validationService.ValidateStopLoss(symbol, entryPrice, stopLoss, accountEquity);
            if (!result.IsValid)
            {
                _logger.LogWarning("Stop loss validation failed: {Errors}",
                    string.Join(", ", result.ValidationErrors.Values));
                // Return a default stop loss if validation fails
                return entryPrice * (1 - _config.DefaultStopLossPercent);
            }

            return stopLoss;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating stop loss");
            throw;
        }
    }

    public async Task<decimal> CalculateTakeProfitAsync(string symbol, decimal entryPrice, decimal stopLoss)
    {
        try
        {
            var riskAmount = Math.Abs(entryPrice - stopLoss);
            var takeProfit = entryPrice + (riskAmount * _config.MinRiskRewardRatio);

            var result = _validationService.ValidateTakeProfit(entryPrice, stopLoss, takeProfit);
            if (!result.IsValid)
            {
                _logger.LogWarning("Take profit validation failed: {Errors}",
                    string.Join(", ", result.ValidationErrors.Values));
                // Return a default take profit if validation fails
                return entryPrice * (1 + _config.DefaultTakeProfitPercent);
            }

            return takeProfit;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating take profit");
            throw;
        }
    }

    public async Task UpdateRiskParametersAsync(Dictionary<string, decimal> parameters)
    {
        try
        {
            var result = _validationService.ValidateRiskParameters(parameters);
            if (!result.IsValid)
            {
                throw new ArgumentException($"Invalid risk parameters: {string.Join(", ", result.ValidationErrors.Values)}");
            }

            foreach (var param in parameters)
            {
                switch (param.Key)
                {
                    case "MaxRiskPerTrade":
                        _config.MaxRiskPerTrade = param.Value;
                        break;
                    case "MaxPortfolioRisk":
                        _config.MaxPortfolioRisk = param.Value;
                        break;
                    case "MaxDrawdown":
                        _config.MaxDrawdown = param.Value;
                        break;
                    case "MinPositionSize":
                        _config.MinPositionSize = param.Value;
                        break;
                    case "MaxPositionSize":
                        _config.MaxPositionSize = param.Value;
                        break;
                    case "MaxLeverage":
                        _config.MaxLeverage = param.Value;
                        break;
                }
            }

            _logger.LogInformation("Updated risk parameters: {Parameters}",
                string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}")));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating risk parameters");
            throw;
        }
    }

    public async Task<IEnumerable<RiskMetrics>> GetHistoricalRiskMetricsAsync(DateTime startTime, DateTime endTime)
    {
        try
        {
            var trades = await _tradeRepository.GetTradeHistory(startTime, endTime);
            var metrics = new List<RiskMetrics>();
            var currentMetrics = new RiskMetrics { AccountEquity = _config.InitialAccountEquity };

            foreach (var trade in trades.OrderBy(t => t.OpenTime))
            {
                currentMetrics = await UpdateMetricsWithTrade(currentMetrics, trade);
                metrics.Add(new RiskMetrics
                {
                    AccountEquity = currentMetrics.AccountEquity,
                    MaxDrawdown = currentMetrics.MaxDrawdown,
                    TotalRisk = currentMetrics.TotalRisk,
                    MarginUsed = currentMetrics.MarginUsed
                });
            }

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting historical risk metrics");
            throw;
        }
    }

    public async Task<Dictionary<string, decimal>> GetRiskLimitsAsync()
    {
        return new Dictionary<string, decimal>
        {
            ["MaxRiskPerTrade"] = _config.MaxRiskPerTrade,
            ["MaxPortfolioRisk"] = _config.MaxPortfolioRisk,
            ["MaxDrawdown"] = _config.MaxDrawdown,
            ["MinPositionSize"] = _config.MinPositionSize,
            ["MaxPositionSize"] = _config.MaxPositionSize,
            ["MaxLeverage"] = _config.MaxLeverage
        };
    }

    public async Task<bool> IsWithinRiskLimitsAsync(Order order)
    {
        try
        {
            var currentMetrics = await GetCurrentRiskMetricsAsync();
            
            // Check if adding this order would exceed any risk limits
            var orderRisk = CalculateOrderRisk(order);
            if (currentMetrics.TotalRisk + orderRisk > _config.MaxPortfolioRisk * currentMetrics.AccountEquity)
            {
                _logger.LogWarning("Order would exceed portfolio risk limit");
                return false;
            }

            var leverage = (currentMetrics.MarginUsed + (order.Price * order.Quantity)) / currentMetrics.AccountEquity;
            if (leverage > _config.MaxLeverage)
            {
                _logger.LogWarning("Order would exceed leverage limit");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking risk limits");
            throw;
        }
    }

    private async Task<decimal> CalculateAccountEquity()
    {
        var openPositions = await _tradeRepository.GetOpenPositions();
        return _config.InitialAccountEquity + openPositions.Sum(p => p.UnrealizedPnL ?? 0);
    }

    private async Task<decimal> CalculateMaxDrawdown()
    {
        var trades = await _tradeRepository.GetTradeHistory(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);
        
        decimal peak = _config.InitialAccountEquity;
        decimal maxDrawdown = 0;
        decimal currentEquity = _config.InitialAccountEquity;

        foreach (var trade in trades.OrderBy(t => t.OpenTime))
        {
            currentEquity += trade.RealizedPnL ?? 0;
            if (currentEquity > peak)
            {
                peak = currentEquity;
            }

            var drawdown = (peak - currentEquity) / peak;
            maxDrawdown = Math.Max(maxDrawdown, drawdown);
        }

        return maxDrawdown;
    }

    private decimal CalculatePositionRisk(Trade position)
    {
        return position.Quantity * Math.Abs(position.EntryPrice - (position.StopLoss ?? position.EntryPrice * 0.95m));
    }

    private decimal CalculateOrderRisk(Order order)
    {
        if (order.StopPrice.HasValue)
        {
            return Math.Abs(order.Price - order.StopPrice.Value) * order.Quantity;
        }
        return order.Price * order.Quantity * _config.DefaultStopLossPercent;
    }

    private async Task<RiskMetrics> UpdateMetricsWithTrade(RiskMetrics metrics, Trade trade)
    {
        if (trade.Status == TradeStatus.Closed)
        {
            metrics.AccountEquity += trade.RealizedPnL ?? 0;
            
            var drawdown = await CalculateMaxDrawdown();
            metrics.MaxDrawdown = Math.Max(metrics.MaxDrawdown, drawdown);
        }
        else
        {
            metrics.TotalRisk += CalculatePositionRisk(trade);
            metrics.MarginUsed += trade.Quantity * trade.EntryPrice;
        }

        return metrics;
    }
}
