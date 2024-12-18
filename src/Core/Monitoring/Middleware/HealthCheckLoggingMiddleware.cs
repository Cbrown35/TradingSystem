using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prometheus;
using TradingSystem.Core.Configuration;
using TradingSystem.Core.Monitoring.Models;
using TradingSystem.Core.Monitoring.Services;

namespace TradingSystem.Core.Monitoring.Middleware;

public class HealthCheckLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HealthCheckLoggingMiddleware> _logger;
    private readonly TradingSystem.Core.Configuration.MonitoringConfig _config;
    private readonly HealthCheckStorageService _storageService;
    private readonly Counter _requestCounter;
    private readonly Histogram _requestDuration;
    private readonly Counter _errorCounter;

    public HealthCheckLoggingMiddleware(
        RequestDelegate next,
        ILogger<HealthCheckLoggingMiddleware> logger,
        IOptions<TradingSystem.Core.Configuration.MonitoringConfig> config,
        HealthCheckStorageService storageService)
    {
        _next = next;
        _logger = logger;
        _config = config.Value;
        _storageService = storageService;

        _requestCounter = Metrics.CreateCounter(
            "healthcheck_requests_total",
            "Total number of health check requests",
            new CounterConfiguration
            {
                LabelNames = new[] { "endpoint", "method", "status" }
            });

        _requestDuration = Metrics.CreateHistogram(
            "healthcheck_request_duration_seconds",
            "Duration of health check requests",
            new HistogramConfiguration
            {
                LabelNames = new[] { "endpoint", "method" }
            });

        _errorCounter = Metrics.CreateCounter(
            "healthcheck_errors_total",
            "Total number of health check errors",
            new CounterConfiguration
            {
                LabelNames = new[] { "endpoint", "code", "message" }
            });
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsHealthCheckEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var sw = Stopwatch.StartNew();
        var originalBodyStream = context.Response.Body;

        try
        {
            using var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            await _next(context);

            memoryStream.Seek(0, SeekOrigin.Begin);
            await memoryStream.CopyToAsync(originalBodyStream);

            sw.Stop();

            // Log request details
            var requestLog = new HealthCheckRequestLog
            {
                Timestamp = DateTime.UtcNow,
                Path = context.Request.Path,
                Method = context.Request.Method,
                StatusCode = context.Response.StatusCode,
                Duration = sw.Elapsed,
                UserAgent = context.Request.Headers["User-Agent"].ToString(),
                ClientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
            };

            // Update metrics
            _requestCounter.WithLabels(
                context.Request.Path,
                context.Request.Method,
                context.Response.StatusCode.ToString()
            ).Inc();

            _requestDuration.WithLabels(
                context.Request.Path,
                context.Request.Method
            ).Observe(sw.Elapsed.TotalSeconds);

            if (context.Response.StatusCode >= 400)
            {
                _errorCounter.WithLabels(
                    context.Request.Path,
                    context.Response.StatusCode.ToString(),
                    "Request failed"
                ).Inc();
            }

            // Store request log if enabled
            if (_config.HealthChecks.Logging.Enabled)
            {
                await StoreRequestLog(requestLog);
            }

            _logger.LogInformation(
                "Health check request: {Method} {Path} {StatusCode} {Duration}ms",
                requestLog.Method,
                requestLog.Path,
                requestLog.StatusCode,
                requestLog.Duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing health check request");
            _errorCounter.WithLabels(
                context.Request.Path,
                "500",
                ex.Message
            ).Inc();

            throw;
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private bool IsHealthCheckEndpoint(PathString path)
    {
        return path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWithSegments("/healthz", StringComparison.OrdinalIgnoreCase);
    }

    private async Task StoreRequestLog(HealthCheckRequestLog requestLog)
    {
        try
        {
            // Store in memory cache or database
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing health check request log");
        }
    }
}

public class HealthCheckRequestLog
{
    public DateTime Timestamp { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public TimeSpan Duration { get; set; }
    public string UserAgent { get; set; } = string.Empty;
    public string ClientIp { get; set; } = string.Empty;
}

public static class HealthCheckLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseHealthCheckLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<HealthCheckLoggingMiddleware>();
    }
}
