namespace TradingSystem.Common.Models;

public enum TimeFrame
{
    Tick,
    Second,
    Minute,
    Hour,
    Day,
    Week,
    Month
}

public static class TimeFrameExtensions
{
    public static TimeSpan ToTimeSpan(this TimeFrame timeFrame)
    {
        return timeFrame switch
        {
            TimeFrame.Tick => TimeSpan.Zero,
            TimeFrame.Second => TimeSpan.FromSeconds(1),
            TimeFrame.Minute => TimeSpan.FromMinutes(1),
            TimeFrame.Hour => TimeSpan.FromHours(1),
            TimeFrame.Day => TimeSpan.FromDays(1),
            TimeFrame.Week => TimeSpan.FromDays(7),
            TimeFrame.Month => TimeSpan.FromDays(30),
            _ => throw new ArgumentException($"Invalid timeframe: {timeFrame}")
        };
    }

    public static string ToIntervalString(this TimeFrame timeFrame)
    {
        return timeFrame switch
        {
            TimeFrame.Tick => "tick",
            TimeFrame.Second => "1s",
            TimeFrame.Minute => "1m",
            TimeFrame.Hour => "1h",
            TimeFrame.Day => "1d",
            TimeFrame.Week => "1w",
            TimeFrame.Month => "1M",
            _ => throw new ArgumentException($"Invalid timeframe: {timeFrame}")
        };
    }
}
