namespace TradingSystem.Common.Models;

public class Theory
{
    public string Name { get; set; } = string.Empty;
    public List<string> Symbols { get; set; } = new();
    public List<Indicator> Indicators { get; set; } = new();
    public Signal OpenSignal { get; set; } = new();
    public Signal CloseSignal { get; set; } = new();
    public Dictionary<string, decimal> Parameters { get; set; } = new();
}
