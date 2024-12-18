namespace TradingSystem.Common.Models;

public enum TradeType
{
    Market,
    Limit,
    StopLoss,
    TakeProfit
}

public enum TradeResult
{
    Win,
    Loss,
    Breakeven,
    Unknown
}

public enum TradeSetup
{
    Trend,
    Reversal,
    Breakout,
    Range,
    Momentum,
    MeanReversion,
    Custom
}
