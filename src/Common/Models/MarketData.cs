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
    public List<OrderBookLevel> OrderBook { get; set; } = new();
    public List<Trade> RecentTrades { get; set; } = new();
}

public class OrderBookLevel
{
    public decimal Price { get; set; }
    public decimal Size { get; set; }
    public int OrderCount { get; set; }
    public bool IsBid { get; set; }
    public DateTime Timestamp { get; set; }

    public static OrderBookLevel FromPriceSize(decimal price, decimal size, bool isBid)
    {
        return new OrderBookLevel
        {
            Price = price,
            Size = size,
            IsBid = isBid,
            Timestamp = DateTime.UtcNow,
            OrderCount = 1 // Default to 1 if not specified
        };
    }

    public static List<OrderBookLevel> FromDictionary(Dictionary<string, decimal> levels, bool isBid)
    {
        var result = new List<OrderBookLevel>();
        foreach (var level in levels)
        {
            if (decimal.TryParse(level.Key, out var price))
            {
                result.Add(FromPriceSize(price, level.Value, isBid));
            }
        }
        return result;
    }

    public static Dictionary<decimal, decimal> ToDictionary(IEnumerable<OrderBookLevel> levels)
    {
        return levels.ToDictionary(l => l.Price, l => l.Size);
    }
}

public class RecentTrade
{
    public decimal Price { get; set; }
    public decimal Size { get; set; }
    public bool IsBuyerMaker { get; set; }
    public DateTime Timestamp { get; set; }
    public string TradeId { get; set; } = string.Empty;
}
