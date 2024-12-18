namespace TradingSystem.Common.Models;

public enum OrderStatus
{
    New,
    PartiallyFilled,
    Filled,
    Cancelled,
    Rejected,
    Expired
}
