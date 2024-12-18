namespace TradingSystem.Common.Models;

public enum OrderTimeInForce
{
    GoodTillCancel,
    ImmediateOrCancel,
    FillOrKill,
    DayOnly
}

public enum OrderVenue
{
    Exchange,
    Broker,
    Market,
    Dark
}
