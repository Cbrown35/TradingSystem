namespace TradingSystem.RealTrading.Configuration;

public class RiskManagerConfig
{
    public decimal MaxRiskPerTrade { get; set; } = 0.02m; // 2% max risk per trade
    public decimal MaxPortfolioRisk { get; set; } = 0.06m; // 6% max portfolio risk
    public decimal MaxDrawdown { get; set; } = 0.20m; // 20% max drawdown
    public decimal MinPositionSize { get; set; } = 0.001m; // Minimum position size
    public decimal MaxPositionSize { get; set; } = 10m; // Maximum position size
    public decimal DefaultStopLossPercent { get; set; } = 0.02m; // 2% default stop loss
    public decimal DefaultTakeProfitPercent { get; set; } = 0.04m; // 4% default take profit
    public decimal MinRiskRewardRatio { get; set; } = 1.5m; // Minimum risk/reward ratio
    public decimal MaxLeverage { get; set; } = 3m; // Maximum leverage
    public decimal InitialAccountEquity { get; set; } = 10000m; // Initial account equity
    public Dictionary<string, decimal> SymbolSpecificLimits { get; set; } = new();
}
