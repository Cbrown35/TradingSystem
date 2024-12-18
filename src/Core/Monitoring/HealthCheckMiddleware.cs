using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Core.Configuration;
using TradingSystem.Core.Monitoring.Interfaces;
using TradingSystem.Core.Monitoring.Models;
using TradingSystem.Core.Monitoring.Services;

namespace TradingSystem.Core.Monitoring;

public class HealthCheckMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HealthCheckMiddleware> _logger;
    private readonly TradingSystem.Core.Configuration.MonitoringConfig _config;
    private readonly IHealthCheckService _healthCheckService;
    private readonly HealthCheckStorageService _storageService;
    private readonly HealthCheckNotificationService _notificationService;

    public HealthCheckMiddleware(
        RequestDelegate next,
        ILogger<HealthCheckMiddleware> logger,
        IOptions<TradingSystem.Core.Configuration.MonitoringConfig> config,
        IHealthCheckService healthCheckService,
        HealthCheckStorageService storageService,
        HealthCheckNotificationService notificationService)
    {
        _next = next;
        _logger = logger;
        _config = config.Value;
        _healthCheckService = healthCheckService;
        _storageService = storageService;
        _notificationService = notificationService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsHealthCheckEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var sw = Stopwatch.StartNew();
        try
        {
            var report = await _healthCheckService.CheckHealthAsync();
            sw.Stop();

            await _storageService.StoreHealthCheckResultAsync(report);

            if (report.Status != Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy)
            {
                await _notificationService.ProcessHealthCheckResultAsync(report);
            }

            await WriteResponseAsync(context, report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing health check request");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new
            {
                Status = "Error",
                Message = "Failed to process health check",
                Error = ex.Message
            });
        }
    }

    private bool IsHealthCheckEndpoint(PathString path)
    {
        return path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWithSegments("/healthz", StringComparison.OrdinalIgnoreCase);
    }

    private async Task WriteResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = report.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy ? 
            StatusCodes.Status200OK : 
            StatusCodes.Status503ServiceUnavailable;

        await context.Response.WriteAsJsonAsync(new
        {
            Status = report.Status.ToString(),
            Duration = report.TotalDuration,
            Entries = report.Entries.ToDictionary(
                e => e.Key,
                e => new
                {
                    Status = e.Value.Status.ToString(),
                    Description = e.Value.Description,
                    Duration = e.Value.Duration,
                    Data = e.Value.Data
                })
        });
    }
}

public static class HealthCheckMiddlewareExtensions
{
    public static IApplicationBuilder UseHealthChecks(this IApplicationBuilder app)
    {
        return app.UseMiddleware<HealthCheckMiddleware>();
    }
}
