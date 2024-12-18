using TradingSystem.Common.Interfaces;
using TradingSystem.Common.Models;

namespace TradingSystem.StrategySearch.Strategies;

public class SimpleMovingAverageStrategy : ITradingStrategy
{
    private int _fastPeriod;
    private int _slowPeriod;
    private readonly string _id;

    public string Id => _id;
    public string Name => "Simple Moving Average Crossover";

    public SimpleMovingAverageStrategy(int fastPeriod = 10, int slowPeriod = 20)
    {
        _fastPeriod = fastPeriod;
        _slowPeriod = slowPeriod;
        _id = $"SMA_{fastPeriod}_{slowPeriod}_{Guid.NewGuid():N}";
    }

    public async Task<bool?> ShouldEnter(List<MarketData> historicalData)
    {
        if (historicalData.Count < _slowPeriod)
            return null;

        var fastSma = CalculateSMA(historicalData, _fastPeriod);
        var slowSma = CalculateSMA(historicalData, _slowPeriod);

        // Check for crossover (fast SMA crosses above slow SMA)
        if (fastSma[^2] <= slowSma[^2] && fastSma[^1] > slowSma[^1])
        {
            return true;
        }

        return false;
    }

    public async Task<bool?> ShouldExit(List<MarketData> historicalData)
    {
        if (historicalData.Count < _slowPeriod)
            return null;

        var fastSma = CalculateSMA(historicalData, _fastPeriod);
        var slowSma = CalculateSMA(historicalData, _slowPeriod);

        // Check for crossunder (fast SMA crosses below slow SMA)
        if (fastSma[^2] >= slowSma[^2] && fastSma[^1] < slowSma[^1])
        {
            return true;
        }

        return false;
    }

    public async Task<Dictionary<string, decimal>> GetParameters()
    {
        return new Dictionary<string, decimal>
        {
            { "FastPeriod", _fastPeriod },
            { "SlowPeriod", _slowPeriod }
        };
    }

    public async Task SetParameters(Dictionary<string, decimal> parameters)
    {
        if (parameters.ContainsKey("FastPeriod"))
            _fastPeriod = (int)parameters["FastPeriod"];

        if (parameters.ContainsKey("SlowPeriod"))  
            _slowPeriod = (int)parameters["SlowPeriod"];
    }

    private List<decimal> CalculateSMA(List<MarketData> data, int period)
    {
        var result = new List<decimal>();
        var prices = data.Select(d => d.Close).ToList();

        for (int i = period - 1; i < prices.Count; i++)
        {
            var sum = 0m;
            for (int j = 0; j < period; j++)
            {
                sum += prices[i - j];
            }
            result.Add(sum / period);
        }

        return result;
    }
}
