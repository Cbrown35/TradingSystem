namespace TradingSystem.Common.Models;

public class Order
{
    public string Id { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public OrderType Type { get; set; }
    public OrderSide Side { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }
    public string ClientOrderId { get; set; } = string.Empty;
    public string ExchangeOrderId { get; set; } = string.Empty;
    public decimal? StopPrice { get; set; }
    public decimal? LimitPrice { get; set; }
    public decimal? AverageFilledPrice { get; set; }
    public decimal FilledQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public decimal Commission { get; set; }
    public string CommissionAsset { get; set; } = string.Empty;
    public TimeInForce TimeInForce { get; set; }
    public bool IsReduceOnly { get; set; }
    public bool IsClosePosition { get; set; }
    public string StrategyName { get; set; } = string.Empty;
    public Guid? TradeId { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
    public string Notes { get; set; } = string.Empty;
    public decimal? Slippage { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public OrderTriggerType? TriggerType { get; set; }
    public string TriggerCondition { get; set; } = string.Empty;
}

public enum TimeInForce
{
    GoodTillCancel,
    ImmediateOrCancel,
    FillOrKill,
    GoodTillDate,
    GoodTillCrossing,
    PostOnly
}
