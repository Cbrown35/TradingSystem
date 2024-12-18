using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TradingSystem.Core.Monitoring.Middleware;

public class HealthCheckCachingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HealthCheckCachingMiddleware> _logger;
    private readonly IDistributedCache _cache;
    private readonly TradingSystem.Core.Configuration.MonitoringConfig _config;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks;

    public HealthCheckCachingMiddleware(
        RequestDelegate next,
        ILogger<HealthCheckCachingMiddleware> logger,
        IDistributedCache cache,
        IOptions<TradingSystem.Core.Configuration.MonitoringConfig> config)
    {
        _next = next;
        _logger = logger;
        _cache = cache;
        _config = config.Value;
        _locks = new ConcurrentDictionary<string, SemaphoreSlim>();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldCacheRequest(context))
        {
            await _next(context);
            return;
        }

        var cacheKey = GetCacheKey(context);
        var lockObj = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));

        await lockObj.WaitAsync();
        try
        {
            var cachedResponse = await GetCachedResponseAsync(cacheKey);
            if (cachedResponse != null)
            {
                await SendCachedResponseAsync(context, cachedResponse);
                return;
            }

            var originalBody = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            var response = await CaptureResponseAsync(context.Response);
            await CacheResponseAsync(cacheKey, response);

            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBody);
        }
        finally
        {
            lockObj.Release();
        }
    }

    private bool ShouldCacheRequest(HttpContext context)
    {
        if (!IsHealthCheckEndpoint(context))
            return false;

        if (context.Request.Method != HttpMethod.Get.Method)
            return false;

        if (context.Request.Headers.ContainsKey("Cache-Control") &&
            context.Request.Headers["Cache-Control"].ToString().Contains("no-cache"))
            return false;

        return true;
    }

    private bool IsHealthCheckEndpoint(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        return path.StartsWith("/health") || 
               path.StartsWith("/metrics") || 
               path.StartsWith("/healthchecks-ui");
    }

    private string GetCacheKey(HttpContext context)
    {
        var keyBuilder = new StringBuilder("healthcheck:");
        keyBuilder.Append(context.Request.Path);
        
        var queryString = context.Request.QueryString.ToString();
        if (!string.IsNullOrEmpty(queryString))
        {
            keyBuilder.Append(queryString);
        }

        return keyBuilder.ToString();
    }

    private async Task<CachedResponse?> GetCachedResponseAsync(string cacheKey)
    {
        try
        {
            var cachedData = await _cache.GetAsync(cacheKey);
            if (cachedData == null)
                return null;

            return JsonSerializer.Deserialize<CachedResponse>(cachedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cached response");
            return null;
        }
    }

    private async Task SendCachedResponseAsync(HttpContext context, CachedResponse cachedResponse)
    {
        context.Response.StatusCode = cachedResponse.StatusCode;
        
        foreach (var header in cachedResponse.Headers)
        {
            context.Response.Headers[header.Key] = header.Value;
        }

        context.Response.Headers["X-Cache"] = "HIT";
        await context.Response.Body.WriteAsync(
            Convert.FromBase64String(cachedResponse.Body));
    }

    private async Task<CachedResponse> CaptureResponseAsync(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);
        var bodyStream = new MemoryStream();
        await response.Body.CopyToAsync(bodyStream);

        var cachedResponse = new CachedResponse
        {
            StatusCode = response.StatusCode,
            Headers = response.Headers.ToDictionary(
                h => h.Key,
                h => h.Value.ToString()),
            Body = Convert.ToBase64String(bodyStream.ToArray())
        };

        return cachedResponse;
    }

    private async Task CacheResponseAsync(string cacheKey, CachedResponse response)
    {
        try
        {
            var cacheEntryOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = 
                    TimeSpan.FromSeconds(_config.HealthChecks.Cache.TimeToLiveSeconds),
                SlidingExpiration = 
                    TimeSpan.FromSeconds(_config.HealthChecks.Cache.SlidingExpirationSeconds)
            };

            var serializedResponse = JsonSerializer.SerializeToUtf8Bytes(response);
            await _cache.SetAsync(cacheKey, serializedResponse, cacheEntryOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching response");
        }
    }
}

public class CachedResponse
{
    public int StatusCode { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public string Body { get; set; } = string.Empty;
}
