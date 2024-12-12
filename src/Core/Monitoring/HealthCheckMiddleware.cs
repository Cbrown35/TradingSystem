using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using TradingSystem.Core.Configuration;

namespace TradingSystem.Core.Monitoring;

public class HealthCheckMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HealthCheckService _healthCheckService;
    private readonly MonitoringConfig _config;
    private readonly ILogger<HealthCheckMiddleware> _logger;

    public HealthCheckMiddleware(
        RequestDelegate next,
        HealthCheckService healthCheckService,
        IOptions<MonitoringConfig> config,
        ILogger<HealthCheckMiddleware> logger)
    {
        _next = next;
        _healthCheckService = healthCheckService;
        _config = config.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await HandleHealthCheckRequest(context);
            return;
        }

        await _next(context);
    }

    private async Task HandleHealthCheckRequest(HttpContext context)
    {
        try
        {
            var report = await _healthCheckService.CheckHealthAsync();
            var path = context.Request.Path.Value?.ToLower();

            switch (context.Request.Headers.Accept.FirstOrDefault())
            {
                case "application/json":
                    await WriteJsonResponse(context, report);
                    break;

                default:
                    if (path?.Contains("ui") == true)
                    {
                        await WriteHtmlResponse(context, report);
                    }
                    else
                    {
                        await WriteJsonResponse(context, report);
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing health checks");
            await HandleHealthCheckError(context, ex);
        }
    }

    private async Task WriteJsonResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = report.Status == HealthStatus.Healthy ? 
            StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;

        var response = new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration,
            timestamp = DateTime.UtcNow,
            entries = report.Entries.Select(e => new
            {
                key = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration,
                data = e.Value.Data,
                error = e.Value.Exception?.Message,
                tags = GetHealthCheckTags(e.Key)
            })
        };

        await JsonSerializer.SerializeAsync(
            context.Response.Body, 
            response, 
            new JsonSerializerOptions { WriteIndented = true }
        );
    }

    private async Task WriteHtmlResponse(HttpContext context, HealthReport report)
    {
        var templatePath = report.Status == HealthStatus.Healthy ?
            "HealthCheckStatus" : "HealthCheckError";

        context.Response.ContentType = "text/html";
        context.Response.StatusCode = report.Status == HealthStatus.Healthy ? 
            StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;

        var viewModel = new
        {
            Report = report,
            Config = _config,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            MachineName = Environment.MachineName,
            ProcessUptime = DateTime.Now - Process.GetCurrentProcess().StartTime,
            Tags = _config.HealthChecks.Tags
        };

        await context.Response.WriteAsync(await RenderTemplate(templatePath, viewModel));
    }

    private async Task HandleHealthCheckError(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "text/html";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        if (context.Request.Headers.Accept.FirstOrDefault() == "application/json")
        {
            var error = new
            {
                status = "Error",
                timestamp = DateTime.UtcNow,
                error = exception.Message,
                details = exception.StackTrace
            };

            await JsonSerializer.SerializeAsync(
                context.Response.Body,
                error,
                new JsonSerializerOptions { WriteIndented = true }
            );
        }
        else
        {
            await context.Response.WriteAsync(
                await RenderTemplate("HealthCheckError", exception)
            );
        }
    }

    private string[] GetHealthCheckTags(string healthCheckName)
    {
        return _config.HealthChecks.Tags
            .Where(t => t.HealthChecks.Contains(healthCheckName))
            .Select(t => t.Name)
            .ToArray();
    }

    private async Task<string> RenderTemplate(string templateName, object model)
    {
        var assembly = typeof(HealthCheckMiddleware).Assembly;
        var templatePath = $"wwwroot/templates/{templateName}.cshtml";
        
        using var stream = assembly.GetManifestResourceStream(templatePath);
        if (stream == null)
        {
            throw new InvalidOperationException($"Template {templateName} not found");
        }

        using var reader = new StreamReader(stream);
        var template = await reader.ReadToEndAsync();

        // In a real implementation, you would use a proper template engine like RazorLight
        // This is a simplified placeholder that would need to be replaced
        return template.Replace("@Model", JsonSerializer.Serialize(model));
    }
}

public static class HealthCheckMiddlewareExtensions
{
    public static IApplicationBuilder UseHealthChecks(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<HealthCheckMiddleware>();
    }
}
