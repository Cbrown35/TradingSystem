using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
}
