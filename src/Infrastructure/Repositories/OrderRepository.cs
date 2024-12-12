using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradingSystem.Common.Interfaces;
using TradingSystem.Common.Models;
using TradingSystem.Infrastructure.Data;

namespace TradingSystem.Infrastructure.Repositories;

public class OrderRepository : RepositoryBase<Order>, IOrderRepository
{
    public OrderRepository(
        TradingContext context,
        ILogger<OrderRepository> logger) : base(context, logger)
    {
    }

    public async Task<Order> AddOrder(Order order)
    {
        return await AddAsync(order);
    }

    public async Task<Order> UpdateOrder(Order order)
    {
        return await UpdateAsync(order);
    }

    public async Task<Order?> GetOrder(string id)
    {
        return await FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<Order?> GetOrderByClientId(string clientOrderId)
    {
        return await FirstOrDefaultAsync(o => o.ClientOrderId == clientOrderId);
    }

    public async Task<Order?> GetOrderByExchangeId(string exchangeOrderId)
    {
        return await FirstOrDefaultAsync(o => o.ExchangeOrderId == exchangeOrderId);
    }

    public async Task<List<Order>> GetOpenOrders(string? symbol = null)
    {
        var query = DbSet.AsQueryable()
            .Where(o => o.Status == OrderStatus.New || 
                       o.Status == OrderStatus.PartiallyFilled);

        if (!string.IsNullOrEmpty(symbol))
        {
            query = query.Where(o => o.Symbol == symbol);
        }

        return await query
            .OrderByDescending(o => o.CreateTime)
            .ToListAsync();
    }

    public async Task<List<Order>> GetOrderHistory(
        DateTime startDate,
        DateTime endDate,
        string? symbol = null,
        string? strategyName = null)
    {
        var query = DbSet.AsQueryable()
            .Where(o => o.CreateTime >= startDate && 
                       o.CreateTime <= endDate);

        if (!string.IsNullOrEmpty(symbol))
        {
            query = query.Where(o => o.Symbol == symbol);
        }

        if (!string.IsNullOrEmpty(strategyName))
        {
            query = query.Where(o => o.StrategyName == strategyName);
        }

        return await query
            .OrderByDescending(o => o.CreateTime)
            .ToListAsync();
    }

    public async Task<List<Order>> GetOrdersByTradeId(Guid tradeId)
    {
        return await DbSet.AsQueryable()
            .Where(o => o.TradeId == tradeId)
            .OrderByDescending(o => o.CreateTime)
            .ToListAsync();
    }

    public async Task<List<Order>> GetOrdersByStatus(OrderStatus status, string? symbol = null)
    {
        var query = DbSet.AsQueryable()
            .Where(o => o.Status == status);

        if (!string.IsNullOrEmpty(symbol))
        {
            query = query.Where(o => o.Symbol == symbol);
        }

        return await query
            .OrderByDescending(o => o.CreateTime)
            .ToListAsync();
    }

    public async Task<Dictionary<string, List<Order>>> GetOrdersByStrategy(List<string> strategyNames)
    {
        var orders = await DbSet.AsQueryable()
            .Where(o => strategyNames.Contains(o.StrategyName!))
            .OrderByDescending(o => o.CreateTime)
            .ToListAsync();

        return orders.GroupBy(o => o.StrategyName!)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public async Task<OrderPerformanceMetrics> GetOrderPerformanceMetrics(
        DateTime startDate,
        DateTime endDate,
        string? symbol = null,
        string? strategyName = null)
    {
        var query = DbSet.AsQueryable()
            .Where(o => o.CreateTime >= startDate && 
                       o.CreateTime <= endDate);

        if (!string.IsNullOrEmpty(symbol))
        {
            query = query.Where(o => o.Symbol == symbol);
        }

        if (!string.IsNullOrEmpty(strategyName))
        {
            query = query.Where(o => o.StrategyName == strategyName);
        }

        var orders = await query.ToListAsync();
        var metrics = new OrderPerformanceMetrics
        {
            TotalOrders = orders.Count,
            FilledOrders = orders.Count(o => o.Status == OrderStatus.Filled),
            PartiallyFilledOrders = orders.Count(o => o.Status == OrderStatus.PartiallyFilled),
            CanceledOrders = orders.Count(o => o.Status == OrderStatus.Canceled),
            RejectedOrders = orders.Count(o => o.Status == OrderStatus.Rejected),
            AverageSlippage = orders.Where(o => o.Slippage.HasValue).Average(o => o.Slippage.Value),
            TotalCommission = orders.Sum(o => o.Commission),
            AverageFillRate = orders.Where(o => o.FilledQuantity > 0)
                .Average(o => o.FilledQuantity / o.Quantity)
        };

        // Basic distributions
        metrics.OrderTypeDistribution = orders.GroupBy(o => o.Type)
            .ToDictionary(g => g.Key, g => g.Count());
        metrics.OrderSideDistribution = orders.GroupBy(o => o.Side)
            .ToDictionary(g => g.Key, g => g.Count());
        metrics.StatusDistribution = orders.GroupBy(o => o.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        // Time-based metrics
        metrics.AverageProcessingTime = orders.Where(o => o.UpdateTime.HasValue)
            .Average(o => (o.UpdateTime.Value - o.CreateTime).TotalMilliseconds);
        metrics.ProcessingTimeDistribution = orders.Where(o => o.UpdateTime.HasValue)
            .GroupBy(o => TimeSpan.FromMilliseconds(Math.Round((o.UpdateTime.Value - o.CreateTime).TotalMilliseconds / 100) * 100))
            .ToDictionary(g => g.Key, g => g.Count());

        // Symbol and timing distributions
        metrics.SymbolDistribution = orders.GroupBy(o => o.Symbol)
            .ToDictionary(g => g.Key, g => g.Count());
        metrics.HourlyDistribution = orders.GroupBy(o => o.CreateTime.Hour)
            .ToDictionary(g => g.Key, g => g.Count());
        metrics.DayOfWeekDistribution = orders.GroupBy(o => o.CreateTime.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.Count());

        // Performance distributions
        metrics.SlippageDistribution = orders.Where(o => o.Slippage.HasValue)
            .GroupBy(o => Math.Round(o.Slippage.Value, 4))
            .ToDictionary(g => g.Key, g => g.Count());
        metrics.CommissionDistribution = orders.GroupBy(o => Math.Round(o.Commission, 4))
            .ToDictionary(g => g.Key, g => g.Count());

        // Strategy and venue performance
        metrics.StrategyPerformance = orders.GroupBy(o => o.StrategyName)
            .ToDictionary(g => g.Key, g => CalculateStrategyPerformance(g.ToList()));

        // Venue metrics
        var venueMetrics = orders.GroupBy(o => o.ExchangeOrderId.Split('-')[0]) // Assuming venue identifier is prefix of exchange order id
            .ToDictionary(g => g.Key, g => CalculateVenueMetrics(g.ToList()));
        metrics.VenueMetrics = venueMetrics;

        // Aggregate venue performance
        metrics.VenuePerformance = venueMetrics.ToDictionary(
            v => v.Key,
            v => v.Value.SuccessfulOrders / (decimal)v.Value.TotalOrders * 100);
        metrics.VenueFillRates = venueMetrics.ToDictionary(
            v => v.Key,
            v => (double)v.Value.FillRate);
        metrics.VenueLatency = venueMetrics.ToDictionary(
            v => v.Key,
            v => v.Value.AverageLatency);

        return metrics;
    }

    public async Task<List<Order>> GetRecentOrders(int count)
    {
        return await DbSet.AsQueryable()
            .OrderByDescending(o => o.CreateTime)
            .Take(count)
            .ToListAsync();
    }

    public async Task DeleteOrder(string id)
    {
        var order = await GetOrder(id);
        if (order != null)
        {
            await DeleteAsync(order);
        }
    }

    private decimal CalculateStrategyPerformance(List<Order> orders)
    {
        var filledOrders = orders.Where(o => o.Status == OrderStatus.Filled).ToList();
        if (!filledOrders.Any()) return 0;

        var totalValue = filledOrders.Sum(o => o.FilledQuantity * o.AverageFilledPrice!.Value);
        var totalCost = filledOrders.Sum(o => o.Commission);
        return (totalValue - totalCost) / totalValue * 100;
    }

    private OrderVenueMetrics CalculateVenueMetrics(List<Order> orders)
    {
        var metrics = new OrderVenueMetrics
        {
            TotalOrders = orders.Count,
            SuccessfulOrders = orders.Count(o => o.Status == OrderStatus.Filled),
            FailedOrders = orders.Count(o => o.Status == OrderStatus.Rejected || o.Status == OrderStatus.Expired),
            AverageLatency = (decimal)orders.Where(o => o.UpdateTime.HasValue)
                .Average(o => (o.UpdateTime.Value - o.CreateTime).TotalMilliseconds),
            AverageSlippage = orders.Where(o => o.Slippage.HasValue)
                .Average(o => o.Slippage!.Value),
            AverageCommission = orders.Average(o => o.Commission),
            FillRate = orders.Where(o => o.FilledQuantity > 0)
                .Average(o => o.FilledQuantity / o.Quantity)
        };

        // Distributions
        metrics.SupportedOrderTypes = orders.GroupBy(o => o.Type)
            .ToDictionary(g => g.Key, g => g.Count());
        metrics.StatusDistribution = orders.GroupBy(o => o.Status)
            .ToDictionary(g => g.Key, g => g.Count());
        metrics.ErrorDistribution = orders.Where(o => !string.IsNullOrEmpty(o.ErrorMessage))
            .GroupBy(o => o.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.Count());

        // Time-based distributions
        metrics.LatencyDistribution = orders.Where(o => o.UpdateTime.HasValue)
            .GroupBy(o => TimeSpan.FromMilliseconds(Math.Round((o.UpdateTime.Value - o.CreateTime).TotalMilliseconds / 100) * 100))
            .ToDictionary(g => g.Key, g => g.Count());
        metrics.HourlyFillRates = orders.GroupBy(o => o.CreateTime.Hour)
            .ToDictionary(g => g.Key, g => CalculateGroupFillRate(g.ToList()));
        metrics.DailyFillRates = orders.GroupBy(o => o.CreateTime.DayOfWeek)
            .ToDictionary(g => g.Key, g => CalculateGroupFillRate(g.ToList()));

        // Size and cost distributions
        metrics.OrderSizeDistribution = orders.GroupBy(o => Math.Round(o.Quantity, 2))
            .ToDictionary(g => g.Key, g => g.Count());
        metrics.CommissionTiers = orders.GroupBy(o => Math.Round(o.Commission, 4))
            .ToDictionary(g => g.Key, g => g.Count());

        // Symbol-specific metrics
        metrics.SymbolLiquidity = orders.GroupBy(o => o.Symbol)
            .ToDictionary(g => g.Key, g => CalculateSymbolLiquidity(g.ToList()));
        metrics.SymbolSpread = orders.GroupBy(o => o.Symbol)
            .ToDictionary(g => g.Key, g => CalculateSymbolSpread(g.ToList()));

        return metrics;
    }

    private decimal CalculateGroupFillRate(List<Order> orders)
    {
        if (!orders.Any()) return 0;
        return orders.Where(o => o.FilledQuantity > 0)
            .Average(o => o.FilledQuantity / o.Quantity);
    }

    private decimal CalculateSymbolLiquidity(List<Order> orders)
    {
        if (!orders.Any()) return 0;
        return orders.Where(o => o.Status == OrderStatus.Filled)
            .Average(o => o.FilledQuantity);
    }

    private decimal CalculateSymbolSpread(List<Order> orders)
    {
        var filledOrders = orders.Where(o => o.Status == OrderStatus.Filled && o.AverageFilledPrice.HasValue)
            .OrderBy(o => o.CreateTime)
            .ToList();

        if (filledOrders.Count < 2) return 0;

        var spreads = new List<decimal>();
        for (int i = 1; i < filledOrders.Count; i++)
        {
            var spread = Math.Abs(filledOrders[i].AverageFilledPrice!.Value - filledOrders[i - 1].AverageFilledPrice!.Value)
                / filledOrders[i - 1].AverageFilledPrice!.Value;
            spreads.Add(spread);
        }

        return spreads.Average();
    }
}
