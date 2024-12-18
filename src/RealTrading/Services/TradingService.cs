using TradingSystem.Common.Interfaces;
using TradingSystem.Common.Models;

namespace TradingSystem.RealTrading.Services;

public class TradingService : ITradingService
{
    private readonly ITradeRepository _tradeRepository;
    private readonly IMarketDataService _marketDataService;
    private readonly IExchangeAdapter _exchangeAdapter;
    private readonly IRiskManager _riskManager;
    private readonly Dictionary<string, ITradingStrategy> _activeStrategies;

    public TradingService(
        ITradeRepository tradeRepository,
        IMarketDataService marketDataService,
        IExchangeAdapter exchangeAdapter,
        IRiskManager riskManager)
    {
        _tradeRepository = tradeRepository;
        _marketDataService = marketDataService;
        _exchangeAdapter = exchangeAdapter;
        _riskManager = riskManager;
        _activeStrategies = new Dictionary<string, ITradingStrategy>();
    }

    public async Task<IEnumerable<ITradingStrategy>> GetActiveStrategiesAsync()
    {
        return _activeStrategies.Values;
    }

    public async Task<StrategyPerformance> GetStrategyPerformanceAsync(string strategyId)
    {
        var trades = await GetStrategyTradesAsync(strategyId);
        var winningTrades = trades.Where(t => (t.RealizedPnL ?? 0) > 0);
        var losingTrades = trades.Where(t => (t.RealizedPnL ?? 0) <= 0);

        return new StrategyPerformance
        {
            IsExecuting = _activeStrategies.ContainsKey(strategyId),
            TotalTrades = trades.Count(),
            SuccessfulTrades = winningTrades.Count(),
            WinRate = trades.Any() ? (decimal)winningTrades.Count() / trades.Count() : 0,
            ProfitFactor = losingTrades.Any() ? 
                Math.Abs(winningTrades.Sum(t => t.RealizedPnL ?? 0) / losingTrades.Sum(t => t.RealizedPnL ?? 0)) : 0,
            MaxDrawdown = CalculateMaxDrawdown(trades),
            ReturnOnInvestment = CalculateROI(trades)
        };
    }

    public async Task StartStrategyAsync(ITradingStrategy strategy)
    {
        _activeStrategies[strategy.Name] = strategy;
        await ProcessStrategy(strategy);
    }

    public async Task StopStrategyAsync(string strategyId)
    {
        if (_activeStrategies.ContainsKey(strategyId))
        {
            _activeStrategies.Remove(strategyId);
            await CloseAllPositionsForStrategy(strategyId);
        }
    }

    public async Task PauseStrategyAsync(string strategyId)
    {
        // Pause strategy execution but keep positions open
        if (_activeStrategies.ContainsKey(strategyId))
        {
            _activeStrategies.Remove(strategyId);
        }
    }

    public async Task ResumeStrategyAsync(string strategyId)
    {
        var strategy = _activeStrategies.GetValueOrDefault(strategyId);
        if (strategy != null)
        {
            await ProcessStrategy(strategy);
        }
    }

    public async Task UpdateStrategyParametersAsync(string strategyId, Dictionary<string, decimal> parameters)
    {
        if (_activeStrategies.TryGetValue(strategyId, out var strategy))
        {
            await strategy.SetParameters(parameters);
        }
    }

    public async Task<IEnumerable<Trade>> GetStrategyTradesAsync(string strategyId, DateTime? startTime = null, DateTime? endTime = null)
    {
        var trades = (await _tradeRepository.GetTradesByStrategy(new List<string> { strategyId }))
            .GetValueOrDefault(strategyId, new List<Trade>());

        return trades.Where(t => 
            (!startTime.HasValue || t.OpenTime >= startTime.Value) &&
            (!endTime.HasValue || t.OpenTime <= endTime.Value));
    }

    public async Task<IEnumerable<Order>> GetStrategyOrdersAsync(string strategyId, DateTime? startTime = null, DateTime? endTime = null)
    {
        var trades = await GetStrategyTradesAsync(strategyId, startTime, endTime);
        return trades.Select(t => new Order 
        { 
            Symbol = t.Symbol,
            Quantity = t.Quantity,
            Price = t.EntryPrice,
            Side = t.Direction == TradeDirection.Long ? OrderSide.Buy : OrderSide.Sell,
            Type = OrderType.Market,
            Status = t.Status == TradeStatus.Open ? OrderStatus.Filled : OrderStatus.Cancelled,
            StopPrice = t.StopLoss,
            LimitPrice = t.TakeProfit,
            StrategyName = t.StrategyName
        });
    }

    public async Task<decimal> GetStrategyPnLAsync(string strategyId, DateTime? startTime = null, DateTime? endTime = null)
    {
        var trades = await GetStrategyTradesAsync(strategyId, startTime, endTime);
        return trades.Sum(t => t.RealizedPnL ?? 0);
    }

    public async Task<Dictionary<string, decimal>> GetStrategyMetricsAsync(string strategyId)
    {
        var performance = await GetStrategyPerformanceAsync(strategyId);
        return new Dictionary<string, decimal>
        {
            ["WinRate"] = performance.WinRate,
            ["ProfitFactor"] = performance.ProfitFactor,
            ["MaxDrawdown"] = performance.MaxDrawdown,
            ["ROI"] = performance.ReturnOnInvestment
        };
    }

    private async Task ProcessStrategy(ITradingStrategy strategy)
    {
        var openPositions = await _tradeRepository.GetOpenPositions();
        foreach (var position in openPositions)
        {
            var marketData = await _marketDataService.GetLatestMarketDataAsync(position.Symbol);
            if (marketData != null)
            {
                var historicalData = (await _marketDataService.GetHistoricalDataAsync(
                    position.Symbol,
                    DateTime.UtcNow.AddDays(-1),
                    DateTime.UtcNow,
                    TimeFrame.Day)).ToList();

                var shouldExit = await strategy.ShouldExit(historicalData);
                if (shouldExit == true)
                {
                    await ClosePosition(position.Symbol, position.Quantity);
                }
            }
        }
    }

    private async Task CloseAllPositionsForStrategy(string strategyId)
    {
        var positions = await _tradeRepository.GetOpenPositions();
        foreach (var position in positions.Where(p => p.StrategyName == strategyId))
        {
            await ClosePosition(position.Symbol, position.Quantity);
        }
    }

    private async Task ClosePosition(string symbol, decimal quantity)
    {
        var openPositions = await _tradeRepository.GetOpenPositions();
        var position = openPositions.FirstOrDefault(p => p.Symbol == symbol);
        if (position != null)
        {
            var closedOrder = await _exchangeAdapter.CloseOrder(position.Id.ToString());
            position.Status = TradeStatus.Closed;
            position.CloseTime = DateTime.UtcNow;
            position.RealizedPnL = closedOrder.RealizedPnL;

            await _tradeRepository.UpdateTrade(position);
        }
    }

    private decimal CalculateMaxDrawdown(IEnumerable<Trade> trades)
    {
        if (!trades.Any()) return 0;

        decimal peak = 0;
        decimal maxDrawdown = 0;
        decimal currentValue = 0;

        foreach (var trade in trades.OrderBy(t => t.OpenTime))
        {
            currentValue += trade.RealizedPnL ?? 0;
            if (currentValue > peak)
            {
                peak = currentValue;
            }

            var drawdown = peak > 0 ? (peak - currentValue) / peak : 0;
            maxDrawdown = Math.Max(maxDrawdown, drawdown);
        }

        return maxDrawdown;
    }

    private decimal CalculateROI(IEnumerable<Trade> trades)
    {
        if (!trades.Any()) return 0;

        var totalPnL = trades.Sum(t => t.RealizedPnL ?? 0);
        var firstTrade = trades.First();
        var initialCapital = firstTrade.Quantity * firstTrade.EntryPrice;

        return initialCapital > 0 ? totalPnL / initialCapital : 0;
    }
}
