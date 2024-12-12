using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using TradingSystem.Core.Configuration;
using TradingSystem.Core.Monitoring.Services;

namespace TradingSystem.Core.Monitoring.Middleware;

public class HealthCheckLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HealthCheckLoggingMiddleware> _logger;
    private readonly MonitoringConfig _config;
    private readonly HealthCheckStorageService _storageService;
    private readonly DiagnosticSource _diagnosticSource;

    public HealthCheckLoggingMiddleware(
        RequestDelegate next,
        ILogger<HealthCheckLoggingMiddleware> logger,
        IOptions<MonitoringConfig> config,
        HealthCheckStorageService storageService)
    {
        _next = next;
        _logger = logger;
        _config = config.Value;
        _storageService = storageService;
        _diagnosticSource = new DiagnosticListener("TradingSystem.HealthChecks");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsHealthCheckEndpoint(context))
        {
            await _next(context);
            return;
        }

        var requestInfo = new HealthCheckRequestInfo
        {
            Timestamp = DateTime.UtcNow,
            Path = context.Request.Path,
            Method = context.Request.Method,
            ClientIp = context.Connection.RemoteIpAddress?.ToString(),
            UserId = context.User?.Identity?.Name,
            UserAgent = context.Request.Headers.UserAgent.ToString(),
            QueryString = context.Request.QueryString.ToString()
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Start diagnostic activity
            using var activity = StartActivity(context, requestInfo);

            // Capture the original body stream
            var originalBodyStream = context.Response.Body;

            // Create a new memory stream
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            stopwatch.Stop();
            requestInfo.Duration = stopwatch.Elapsed;
            requestInfo.StatusCode = context.Response.StatusCode;

            // Read the response body
            responseBody.Seek(0, SeekOrigin.Begin);
            var responseContent = await new StreamReader(responseBody).ReadToEndAsync();
            requestInfo.ResponseContent = responseContent;

            // Copy the response to the original stream
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);

            // Restore the original stream
            context.Response.Body = originalBodyStream;

            await LogHealthCheckRequest(requestInfo);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            requestInfo.Duration = stopwatch.Elapsed;
            requestInfo.StatusCode = 500;
            requestInfo.Error = ex.Message;

            _logger.LogError(ex, "Error processing health check request");
            throw;
        }
        finally
        {
            EmitMetrics(requestInfo);
        }
    }

    private bool IsHealthCheckEndpoint(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        return path.StartsWith("/health") || 
               path.StartsWith("/metrics") || 
               path.StartsWith("/healthchecks-ui");
    }

    private Activity? StartActivity(HttpContext context, HealthCheckRequestInfo requestInfo)
    {
        var activity = new Activity("HealthCheck");
        
        if (activity.IsAllDataRequested)
        {
            activity.SetTag("http.method", context.Request.Method);
            activity.SetTag("http.url", context.Request.Path);
            activity.SetTag("http.client_ip", requestInfo.ClientIp);
            activity.SetTag("user.id", requestInfo.UserId);
            activity.SetTag("user.agent", requestInfo.UserAgent);
        }

        return _diagnosticSource.StartActivity(activity, requestInfo);
    }

    private async Task LogHealthCheckRequest(HealthCheckRequestInfo requestInfo)
    {
        // Log to application insights or other logging provider
        _logger.LogInformation(
            "Health check request: {Path} {Method} {StatusCode} {Duration}ms",
            requestInfo.Path,
            requestInfo.Method,
            requestInfo.StatusCode,
            requestInfo.Duration.TotalMilliseconds);

        // Store in database if configured
        if (_config.HealthChecks.Logging.StoreInDatabase)
        {
            await _storageService.StoreRequestLog(requestInfo);
        }

        // Write to file if configured
        if (_config.HealthChecks.Logging.WriteToFile)
        {
            await WriteToLogFile(requestInfo);
        }
    }

    private async Task WriteToLogFile(HealthCheckRequestInfo requestInfo)
    {
        var logEntry = $"{requestInfo.Timestamp:O}|{requestInfo.Path}|{requestInfo.Method}|" +
                      $"{requestInfo.StatusCode}|{requestInfo.Duration.TotalMilliseconds}ms|" +
                      $"{requestInfo.ClientIp}|{requestInfo.UserId}|{requestInfo.Error}\n";

        var logPath = _config.HealthChecks.Logging.LogFilePath;
        await File.AppendAllTextAsync(logPath, logEntry);
    }

    private void EmitMetrics(HealthCheckRequestInfo requestInfo)
    {
        MetricsConfig.HealthCheckRequests.Add(1, new TagList
        {
            { "path", requestInfo.Path },
            { "method", requestInfo.Method },
            { "status_code", requestInfo.StatusCode.ToString() }
        });

        MetricsConfig.HealthCheckDuration.Record(
            requestInfo.Duration.TotalSeconds,
            new TagList
            {
                { "path", requestInfo.Path },
                { "method", requestInfo.Method }
            });

        if (requestInfo.StatusCode >= 400)
        {
            MetricsConfig.HealthCheckErrors.Add(1, new TagList
            {
                { "path", requestInfo.Path },
                { "status_code", requestInfo.StatusCode.ToString() },
                { "error", requestInfo.Error ?? "Unknown" }
            });
        }
    }
}

public class HealthCheckRequestInfo
{
    public DateTime Timestamp { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ClientIp { get; set; }
    public string? UserId { get; set; }
    public string? UserAgent { get; set; }
    public string? QueryString { get; set; }
    public string? ResponseContent { get; set; }
    public string? Error { get; set; }
}

public static class HealthCheckLoggingExtensions
{
    public static IApplicationBuilder UseHealthCheckLogging(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<HealthCheckLoggingMiddleware>();
    }
}
