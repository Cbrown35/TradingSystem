using System.Diagnostics;
using System.IO.Compression;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using TradingSystem.Core.Configuration;

namespace TradingSystem.Core.Monitoring.Middleware;

public static class HealthCheckMiddlewarePipeline
{
    public static IApplicationBuilder UseHealthCheckPipeline(this IApplicationBuilder app)
    {
        var config = app.ApplicationServices
            .GetRequiredService<IOptions<MonitoringConfig>>()
            .Value;

        // Add rate limiting if enabled
        if (config.HealthChecks.RateLimit.Enabled)
        {
            app.UseMiddleware<HealthCheckRateLimitMiddleware>();
        }

        // Add caching if enabled
        if (config.HealthChecks.Cache.Enabled)
        {
            app.UseMiddleware<HealthCheckCachingMiddleware>();
        }

        // Add compression if enabled
        if (config.HealthChecks.Compression.Enabled)
        {
            app.UseMiddleware<HealthCheckCompressionMiddleware>();
        }

        // Add logging if enabled
        if (config.HealthChecks.Logging.Enabled)
        {
            app.UseMiddleware<HealthCheckLoggingMiddleware>();
        }

        return app;
    }

    public static IApplicationBuilder UseHealthCheckRateLimit(this IApplicationBuilder app)
    {
        return app.UseMiddleware<HealthCheckRateLimitMiddleware>();
    }

    public static IApplicationBuilder UseHealthCheckCaching(this IApplicationBuilder app)
    {
        return app.UseMiddleware<HealthCheckCachingMiddleware>();
    }

    public static IApplicationBuilder UseHealthCheckCompression(this IApplicationBuilder app)
    {
        return app.UseMiddleware<HealthCheckCompressionMiddleware>();
    }

    public static IApplicationBuilder UseHealthCheckLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<HealthCheckLoggingMiddleware>();
    }
}

public class HealthCheckRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HealthCheckRateLimitMiddleware> _logger;
    private readonly MonitoringConfig _config;
    private readonly ConcurrentDictionary<string, TokenBucket> _buckets = new();

    public HealthCheckRateLimitMiddleware(
        RequestDelegate next,
        ILogger<HealthCheckRateLimitMiddleware> logger,
        IOptions<MonitoringConfig> config)
    {
        _next = next;
        _logger = logger;
        _config = config.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (_config.HealthChecks.RateLimit.WhitelistedIPs.Contains(clientIp))
        {
            await _next(context);
            return;
        }

        var bucket = _buckets.GetOrAdd(clientIp, _ => new TokenBucket(
            _config.HealthChecks.RateLimit.RequestsPerMinute,
            _config.HealthChecks.RateLimit.BurstSize));

        if (!bucket.TryTake())
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Too many requests",
                retryAfter = bucket.GetRetryAfter()
            });
            return;
        }

        await _next(context);
    }

    private class TokenBucket
    {
        private readonly int _refillRate;
        private readonly int _capacity;
        private double _tokens;
        private DateTime _lastRefill;
        private readonly object _lock = new();

        public TokenBucket(int refillRate, int capacity)
        {
            _refillRate = refillRate;
            _capacity = capacity;
            _tokens = capacity;
            _lastRefill = DateTime.UtcNow;
        }

        public bool TryTake()
        {
            lock (_lock)
            {
                RefillTokens();

                if (_tokens < 1)
                {
                    return false;
                }

                _tokens--;
                return true;
            }
        }

        public TimeSpan GetRetryAfter()
        {
            lock (_lock)
            {
                RefillTokens();
                var tokensNeeded = 1 - _tokens;
                if (tokensNeeded <= 0)
                {
                    return TimeSpan.Zero;
                }

                return TimeSpan.FromSeconds(tokensNeeded / _refillRate * 60);
            }
        }

        private void RefillTokens()
        {
            var now = DateTime.UtcNow;
            var elapsed = (now - _lastRefill).TotalMinutes;
            var tokensToAdd = elapsed * _refillRate;

            _tokens = Math.Min(_capacity, _tokens + tokensToAdd);
            _lastRefill = now;
        }
    }
}

public class HealthCheckCachingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HealthCheckCachingMiddleware> _logger;
    private readonly MonitoringConfig _config;
    private readonly IMemoryCache _cache;

    public HealthCheckCachingMiddleware(
        RequestDelegate next,
        ILogger<HealthCheckCachingMiddleware> logger,
        IOptions<MonitoringConfig> config,
        IMemoryCache cache)
    {
        _next = next;
        _logger = logger;
        _config = config.Value;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.Request.Path.Value ?? "/";

        if (!_config.HealthChecks.Cache.Enabled || !IsHealthCheckEndpoint(endpoint))
        {
            await _next(context);
            return;
        }

        var cacheKey = $"healthcheck:{endpoint}";
        if (_cache.TryGetValue(cacheKey, out var cachedResponse))
        {
            await context.Response.WriteAsJsonAsync(cachedResponse);
            return;
        }

        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context);

        responseBody.Seek(0, SeekOrigin.Begin);
        var response = await new StreamReader(responseBody).ReadToEndAsync();
        responseBody.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalBodyStream);

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(_config.HealthChecks.Cache.TimeToLiveSeconds))
            .SetSlidingExpiration(TimeSpan.FromSeconds(_config.HealthChecks.Cache.SlidingExpirationSeconds));

        _cache.Set(cacheKey, response, cacheOptions);
    }

    private bool IsHealthCheckEndpoint(string endpoint)
    {
        return endpoint.StartsWith("/health", StringComparison.OrdinalIgnoreCase);
    }
}

public class HealthCheckCompressionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HealthCheckCompressionMiddleware> _logger;
    private readonly MonitoringConfig _config;

    public HealthCheckCompressionMiddleware(
        RequestDelegate next,
        ILogger<HealthCheckCompressionMiddleware> logger,
        IOptions<MonitoringConfig> config)
    {
        _next = next;
        _logger = logger;
        _config = config.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context);

        if (!_config.HealthChecks.Compression.Enabled || 
            responseBody.Length < _config.HealthChecks.Compression.MinimumSizeBytes)
        {
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
            return;
        }

        context.Response.Headers.Append("Content-Encoding", 
            _config.HealthChecks.Compression.PreferBrotli ? "br" : "gzip");

        responseBody.Seek(0, SeekOrigin.Begin);
        await CompressStreamAsync(responseBody, originalBodyStream);
    }

    private async Task CompressStreamAsync(Stream source, Stream destination)
    {
        if (_config.HealthChecks.Compression.PreferBrotli)
        {
            using var brotli = new BrotliStream(destination, CompressionLevel.Optimal, true);
            await source.CopyToAsync(brotli);
        }
        else
        {
            using var gzip = new GZipStream(destination, CompressionLevel.Optimal, true);
            await source.CopyToAsync(gzip);
        }
    }
}

public class HealthCheckLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HealthCheckLoggingMiddleware> _logger;
    private readonly MonitoringConfig _config;

    public HealthCheckLoggingMiddleware(
        RequestDelegate next,
        ILogger<HealthCheckLoggingMiddleware> logger,
        IOptions<MonitoringConfig> config)
    {
        _next = next;
        _logger = logger;
        _config = config.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);

            sw.Stop();
            responseBody.Seek(0, SeekOrigin.Begin);
            var response = await new StreamReader(responseBody).ReadToEndAsync();
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);

            LogHealthCheckResult(context, sw.Elapsed, response);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Health check error occurred after {Duration}ms", 
                sw.ElapsedMilliseconds);
            throw;
        }
    }

    private void LogHealthCheckResult(HttpContext context, TimeSpan duration, string response)
    {
        var logLevel = context.Response.StatusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            _ => LogLevel.Information
        };

        _logger.Log(logLevel,
            "Health check {Method} {Path} completed with status {StatusCode} in {Duration}ms",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            duration.TotalMilliseconds);

        if (_config.HealthChecks.Logging.WriteToFile)
        {
            Task.Run(async () =>
            {
                var logEntry = $"{DateTime.UtcNow:O}|{context.Request.Method}|{context.Request.Path}|" +
                             $"{context.Response.StatusCode}|{duration.TotalMilliseconds}ms|{response}";

                await File.AppendAllLinesAsync(
                    _config.HealthChecks.Logging.LogFilePath,
                    new[] { logEntry });
            });
        }
    }
}
