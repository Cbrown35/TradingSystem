namespace TradingSystem.StrategySearch.Services.Backtesting;

internal class ParameterOptimizer
{
    public IEnumerable<Dictionary<string, decimal>> GenerateParameterSets(
        Dictionary<string, (decimal min, decimal max, decimal step)> parameterRanges)
    {
        var parameterSets = new List<Dictionary<string, decimal>>();
        var currentSet = parameterRanges.ToDictionary(p => p.Key, p => p.Value.min);
        
        while (true)
        {
            parameterSets.Add(new Dictionary<string, decimal>(currentSet));
            
            bool done = true;
            foreach (var param in parameterRanges.Keys.ToList())
            {
                var (min, max, step) = parameterRanges[param];
                var current = currentSet[param];
                
                if (current + step <= max)
                {
                    currentSet[param] = current + step;
                    done = false;
                    break;
                }
                else
                {
                    currentSet[param] = min;
                }
            }
            
            if (done) break;
        }
        
        return parameterSets;
    }
}
