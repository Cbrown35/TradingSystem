using TradingSystem.Common.Models;

namespace TradingSystem.Common.Models;

public class MarketStatistics
{
    public string Symbol { get; set; } = string.Empty;
    public TimeSpan Period { get; set; }
    public decimal AverageVolume { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal Volatility { get; set; }
    public decimal TrendStrength { get; set; }
    public decimal HighestPrice { get; set; }
    public decimal LowestPrice { get; set; }
    public decimal DailyRange { get; set; }
    public decimal WeeklyRange { get; set; }
    public decimal MonthlyRange { get; set; }
    public decimal AverageTrueRange { get; set; }
    public decimal RelativeStrengthIndex { get; set; }
    public decimal BollingerBandWidth { get; set; }
    public decimal MarketCap { get; set; }
    public decimal Beta { get; set; }
    public decimal CorrelationWithMarket { get; set; }
    public Dictionary<string, decimal> SectorCorrelations { get; set; } = new();
    public Dictionary<TimeFrame, TradingActivity> ActivityByTimeFrame { get; set; } = new();
}

public class TradingActivity
{
    public decimal Volume { get; set; }
    public decimal Volatility { get; set; }
    public decimal AverageSpread { get; set; }
    public decimal TradeCount { get; set; }
    public decimal LargeOrderCount { get; set; }
    public Dictionary<string, decimal> ParticipantBreakdown { get; set; } = new();
}
