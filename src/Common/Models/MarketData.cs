namespace TradingSystem.Common.Models;

public class MarketData
{
    public string Symbol { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    public decimal? QuoteVolume { get; set; }
    public decimal? OpenInterest { get; set; }
    public decimal? BidPrice { get; set; }
    public decimal? AskPrice { get; set; }
    public decimal? BidSize { get; set; }
    public decimal? AskSize { get; set; }
    public decimal? VWAP { get; set; }
    public int NumberOfTrades { get; set; }
    public TimeFrame Interval { get; set; }
    public Dictionary<string, decimal> Indicators { get; set; } = new();
    public Dictionary<string, decimal> CustomMetrics { get; set; } = new();
    public MarketCondition MarketCondition { get; set; }
    public decimal? ImbalanceRatio { get; set; }
    public decimal? TakerBuyVolume { get; set; }
    public decimal? TakerSellVolume { get; set; }
    public decimal? CumulativeDelta { get; set; }
    public decimal? Volatility { get; set; }
    public decimal? RelativeVolume { get; set; }
    public decimal? SpreadPercentage { get; set; }
    public decimal? LiquidityScore { get; set; }
    public Dictionary<string, decimal> OrderBookLevels { get; set; } = new();
    public List<Trade> RecentTrades { get; set; } = new();
}

public enum MarketCondition
{
    Normal,
    Trending,
    Ranging,
    Volatile,
    Quiet,
    PreMarket,
    PostMarket,
    OpeningRange,
    ClosingRange,
    HighVolume,
    LowVolume,
    BreakoutAttempt,
    Support,
    Resistance,
    Unknown
}

public class OrderBookLevel
{
    public decimal Price { get; set; }
    public decimal Size { get; set; }
    public int OrderCount { get; set; }
    public bool IsBid { get; set; }
    public DateTime Timestamp { get; set; }
}

public class RecentTrade
{
    public decimal Price { get; set; }
    public decimal Size { get; set; }
    public bool IsBuyerMaker { get; set; }
    public DateTime Timestamp { get; set; }
    public string TradeId { get; set; } = string.Empty;
}
