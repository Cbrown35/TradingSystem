using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using TradingSystem.Core.Configuration;

namespace TradingSystem.Core.Monitoring.Middleware;

public class HealthCheckRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HealthCheckRateLimitMiddleware> _logger;
    private readonly MonitoringConfig _config;
    private readonly ConcurrentDictionary<string, TokenBucket> _buckets;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks;

    public HealthCheckRateLimitMiddleware(
        RequestDelegate next,
        ILogger<HealthCheckRateLimitMiddleware> logger,
        IOptions<MonitoringConfig> config)
    {
        _next = next;
        _logger = logger;
        _config = config.Value;
        _buckets = new ConcurrentDictionary<string, TokenBucket>();
        _locks = new ConcurrentDictionary<string, SemaphoreSlim>();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsHealthCheckEndpoint(context))
        {
            await _next(context);
            return;
        }

        var clientId = GetClientIdentifier(context);
        var bucket = _buckets.GetOrAdd(clientId, _ => CreateTokenBucket());
        var lockObj = _locks.GetOrAdd(clientId, _ => new SemaphoreSlim(1, 1));

        await lockObj.WaitAsync();
        try
        {
            if (await bucket.TryConsumeAsync())
            {
                await _next(context);
            }
            else
            {
                _logger.LogWarning("Rate limit exceeded for client {ClientId}", clientId);
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.Headers.Add("Retry-After", GetRetryAfterSeconds(bucket).ToString());
                
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Rate limit exceeded",
                    retryAfter = GetRetryAfterSeconds(bucket),
                    limit = _config.HealthChecks.RateLimit.RequestsPerMinute,
                    window = "1 minute"
                });
            }
        }
        finally
        {
            lockObj.Release();
        }
    }

    private bool IsHealthCheckEndpoint(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        return path.StartsWith("/health") || 
               path.StartsWith("/metrics") || 
               path.StartsWith("/healthchecks-ui");
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Try to get API key from header
        var apiKey = context.Request.Headers["X-API-Key"].ToString();
        if (!string.IsNullOrEmpty(apiKey))
        {
            return $"api:{apiKey}";
        }

        // Try to get authenticated user
        var userId = context.User?.Identity?.Name;
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        // Fall back to IP address
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{ip}";
    }

    private TokenBucket CreateTokenBucket()
    {
        return new TokenBucket(
            _config.HealthChecks.RateLimit.RequestsPerMinute,
            _config.HealthChecks.RateLimit.BurstSize,
            TimeSpan.FromMinutes(1));
    }

    private int GetRetryAfterSeconds(TokenBucket bucket)
    {
        return (int)Math.Ceiling(bucket.GetTimeUntilNextToken().TotalSeconds);
    }
}

public class TokenBucket
{
    private readonly double _refillRate;
    private readonly double _maxTokens;
    private double _tokens;
    private DateTime _lastRefill;
    private readonly object _lock = new();

    public TokenBucket(int tokensPerInterval, int burstSize, TimeSpan interval)
    {
        _maxTokens = burstSize;
        _refillRate = tokensPerInterval / interval.TotalSeconds;
        _tokens = burstSize;
        _lastRefill = DateTime.UtcNow;
    }

    public async Task<bool> TryConsumeAsync()
    {
        await RefillAsync();

        lock (_lock)
        {
            if (_tokens >= 1)
            {
                _tokens--;
                return true;
            }
            return false;
        }
    }

    public TimeSpan GetTimeUntilNextToken()
    {
        lock (_lock)
        {
            if (_tokens >= 1)
                return TimeSpan.Zero;

            var tokensNeeded = 1 - _tokens;
            var secondsNeeded = tokensNeeded / _refillRate;
            return TimeSpan.FromSeconds(secondsNeeded);
        }
    }

    private async Task RefillAsync()
    {
        var now = DateTime.UtcNow;
        var elapsedSeconds = (now - _lastRefill).TotalSeconds;
        
        if (elapsedSeconds < 0.1) // Avoid too frequent updates
            return;

        lock (_lock)
        {
            var tokensToAdd = elapsedSeconds * _refillRate;
            _tokens = Math.Min(_maxTokens, _tokens + tokensToAdd);
            _lastRefill = now;
        }

        await Task.CompletedTask;
    }
}

public static class HealthCheckRateLimitExtensions
{
    public static IApplicationBuilder UseHealthCheckRateLimit(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<HealthCheckRateLimitMiddleware>();
    }
}

public class RateLimitOptions
{
    public int RequestsPerMinute { get; set; } = 60;
    public int BurstSize { get; set; } = 10;
    public Dictionary<string, int> CustomLimits { get; set; } = new();
    public List<string> WhitelistedIPs { get; set; } = new();
    public List<string> WhitelistedUsers { get; set; } = new();
}

public class RateLimitExceededException : Exception
{
    public int RetryAfterSeconds { get; }

    public RateLimitExceededException(int retryAfterSeconds)
        : base($"Rate limit exceeded. Please try again in {retryAfterSeconds} seconds.")
    {
        RetryAfterSeconds = retryAfterSeconds;
    }
}
