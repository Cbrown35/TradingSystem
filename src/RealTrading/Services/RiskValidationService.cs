using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Common.Models;
using TradingSystem.RealTrading.Configuration;

namespace TradingSystem.RealTrading.Services;

public interface IRiskValidationService
{
    ValidationResult ValidateOrder(Order order, RiskMetrics currentMetrics);
    ValidationResult ValidatePositionSize(string symbol, decimal size, decimal accountEquity);
    ValidationResult ValidateDrawdown(decimal drawdown);
    ValidationResult ValidateRiskParameters(Dictionary<string, decimal> parameters);
    ValidationResult ValidateStopLoss(string symbol, decimal entryPrice, decimal stopLoss, decimal accountEquity);
    ValidationResult ValidateTakeProfit(decimal entryPrice, decimal stopLoss, decimal takeProfit);
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, string> ValidationErrors { get; set; } = new();

    public static ValidationResult Success()
        => new() { IsValid = true };

    public static ValidationResult Failure(string message)
        => new() { IsValid = false, ErrorMessage = message };

    public static ValidationResult FromErrors(Dictionary<string, string> errors)
        => new() { IsValid = false, ValidationErrors = errors };
}

public class RiskValidationService : IRiskValidationService
{
    private readonly RiskManagerConfig _config;
    private readonly ILogger<RiskValidationService> _logger;

    public RiskValidationService(
        IOptions<RiskManagerConfig> config,
        ILogger<RiskValidationService> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    public ValidationResult ValidateOrder(Order order, RiskMetrics currentMetrics)
    {
        var errors = new Dictionary<string, string>();

        // Check if order would exceed max portfolio risk
        var orderRisk = CalculateOrderRisk(order);
        if (currentMetrics.TotalRisk + orderRisk > _config.MaxPortfolioRisk * currentMetrics.AccountEquity)
        {
            errors.Add("PortfolioRisk", $"Order would exceed maximum portfolio risk of {_config.MaxPortfolioRisk:P}");
        }

        // Check position size limits
        if (order.Quantity < _config.MinPositionSize)
        {
            errors.Add("PositionSize", $"Position size {order.Quantity} is below minimum of {_config.MinPositionSize}");
        }
        if (order.Quantity > _config.MaxPositionSize)
        {
            errors.Add("PositionSize", $"Position size {order.Quantity} exceeds maximum of {_config.MaxPositionSize}");
        }

        // Check leverage
        var leverage = CalculateLeverage(order, currentMetrics.AccountEquity);
        if (leverage > _config.MaxLeverage)
        {
            errors.Add("Leverage", $"Order leverage {leverage:F2}x exceeds maximum of {_config.MaxLeverage}x");
        }

        // Check symbol-specific limits
        if (_config.SymbolSpecificLimits.TryGetValue(order.Symbol, out var maxSize) && order.Quantity > maxSize)
        {
            errors.Add("SymbolLimit", $"Order size exceeds symbol-specific limit of {maxSize}");
        }

        return errors.Any() 
            ? ValidationResult.FromErrors(errors)
            : ValidationResult.Success();
    }

    public ValidationResult ValidatePositionSize(string symbol, decimal size, decimal accountEquity)
    {
        var errors = new Dictionary<string, string>();

        if (size < _config.MinPositionSize)
        {
            errors.Add("MinSize", $"Position size {size} is below minimum of {_config.MinPositionSize}");
        }

        if (size > _config.MaxPositionSize)
        {
            errors.Add("MaxSize", $"Position size {size} exceeds maximum of {_config.MaxPositionSize}");
        }

        var riskAmount = size * _config.DefaultStopLossPercent;
        var riskPercentage = riskAmount / accountEquity;
        if (riskPercentage > _config.MaxRiskPerTrade)
        {
            errors.Add("RiskPerTrade", $"Position would risk {riskPercentage:P} which exceeds maximum of {_config.MaxRiskPerTrade:P}");
        }

        if (_config.SymbolSpecificLimits.TryGetValue(symbol, out var maxSize) && size > maxSize)
        {
            errors.Add("SymbolLimit", $"Position size exceeds symbol-specific limit of {maxSize}");
        }

        return errors.Any()
            ? ValidationResult.FromErrors(errors)
            : ValidationResult.Success();
    }

    public ValidationResult ValidateDrawdown(decimal drawdown)
    {
        if (drawdown > _config.MaxDrawdown)
        {
            return ValidationResult.Failure(
                $"Drawdown of {drawdown:P} exceeds maximum allowed drawdown of {_config.MaxDrawdown:P}");
        }
        return ValidationResult.Success();
    }

    public ValidationResult ValidateRiskParameters(Dictionary<string, decimal> parameters)
    {
        var errors = new Dictionary<string, string>();

        foreach (var param in parameters)
        {
            switch (param.Key)
            {
                case "MaxRiskPerTrade" when param.Value <= 0 || param.Value > 1:
                    errors.Add(param.Key, "Must be between 0 and 1");
                    break;
                case "MaxPortfolioRisk" when param.Value <= 0 || param.Value > 1:
                    errors.Add(param.Key, "Must be between 0 and 1");
                    break;
                case "MaxDrawdown" when param.Value <= 0 || param.Value > 1:
                    errors.Add(param.Key, "Must be between 0 and 1");
                    break;
                case "MinPositionSize" when param.Value <= 0:
                    errors.Add(param.Key, "Must be greater than 0");
                    break;
                case "MaxPositionSize" when param.Value <= 0:
                    errors.Add(param.Key, "Must be greater than 0");
                    break;
                case "MaxLeverage" when param.Value <= 0:
                    errors.Add(param.Key, "Must be greater than 0");
                    break;
            }
        }

        return errors.Any()
            ? ValidationResult.FromErrors(errors)
            : ValidationResult.Success();
    }

    public ValidationResult ValidateStopLoss(string symbol, decimal entryPrice, decimal stopLoss, decimal accountEquity)
    {
        var errors = new Dictionary<string, string>();

        var stopLossPercent = Math.Abs(stopLoss - entryPrice) / entryPrice;
        if (stopLossPercent > _config.DefaultStopLossPercent * 2)
        {
            errors.Add("StopLoss", $"Stop loss distance of {stopLossPercent:P} exceeds maximum of {_config.DefaultStopLossPercent * 2:P}");
        }

        if (stopLossPercent < _config.DefaultStopLossPercent / 2)
        {
            errors.Add("StopLoss", $"Stop loss distance of {stopLossPercent:P} is below minimum of {_config.DefaultStopLossPercent / 2:P}");
        }

        return errors.Any()
            ? ValidationResult.FromErrors(errors)
            : ValidationResult.Success();
    }

    public ValidationResult ValidateTakeProfit(decimal entryPrice, decimal stopLoss, decimal takeProfit)
    {
        var errors = new Dictionary<string, string>();

        var riskAmount = Math.Abs(entryPrice - stopLoss);
        var rewardAmount = Math.Abs(takeProfit - entryPrice);
        var riskRewardRatio = rewardAmount / riskAmount;

        if (riskRewardRatio < _config.MinRiskRewardRatio)
        {
            errors.Add("RiskReward", $"Risk/reward ratio of {riskRewardRatio:F2} is below minimum of {_config.MinRiskRewardRatio:F2}");
        }

        return errors.Any()
            ? ValidationResult.FromErrors(errors)
            : ValidationResult.Success();
    }

    private decimal CalculateOrderRisk(Order order)
    {
        if (order.StopPrice.HasValue)
        {
            return Math.Abs(order.Price - order.StopPrice.Value) * order.Quantity;
        }
        // If no stop price specified, use default risk percentage
        return order.Price * order.Quantity * _config.DefaultStopLossPercent;
    }

    private decimal CalculateLeverage(Order order, decimal accountEquity)
    {
        var positionValue = order.Price * order.Quantity;
        return positionValue / accountEquity;
    }
}
