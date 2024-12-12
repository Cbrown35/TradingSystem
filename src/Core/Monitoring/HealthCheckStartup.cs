using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Prometheus;
using TradingSystem.Core.Configuration;
using TradingSystem.Core.Monitoring.Auth;
using TradingSystem.Core.Monitoring.HealthChecks;
using TradingSystem.Core.Monitoring.Middleware;
using TradingSystem.Core.Monitoring.Services;
using HealthChecks.UI.Client;

namespace TradingSystem.Core.Monitoring;

public static class HealthCheckStartup
{
    public static IServiceCollection AddTradingSystemHealthChecks(
        this IServiceCollection services,
        MonitoringConfig config)
    {
        // Add health checks
        var healthChecks = services.AddHealthChecks();

        // Core health checks
        healthChecks
            .AddCheck<MarketDataHealthCheck>(
                "market_data",
                tags: new[] { "market_data", "core" })
            .AddCheck<RiskManagementHealthCheck>(
                "risk_management",
                tags: new[] { "risk", "core" })
            .AddCheck<StrategyExecutionHealthCheck>(
                "strategy_execution",
                tags: new[] { "trading", "strategy", "core" });

        // Add system health checks
        healthChecks
            .AddDiskStorageHealthCheck(options =>
            {
                options.AddDrive("C:\\", 1024); // 1GB minimum
            }, "disk_space", tags: new[] { "system" })
            .AddProcessAllocatedMemoryHealthCheck(512, "memory", tags: new[] { "system" })
            .AddProcessHealthCheck("dotnet", "process", tags: new[] { "system" });

        // Add network health checks
        if (config.HealthChecks.Enabled)
        {
            healthChecks.AddPingHealthCheck(options =>
            {
                options.AddHost("8.8.8.8", 1000);
            }, "network", tags: new[] { "network" });
        }

        // Add health checks UI
        services.AddHealthChecksUI(setup =>
        {
            setup.SetEvaluationTimeInSeconds(config.HealthChecks.IntervalSeconds);
            setup.SetApiMaxActiveRequests(1);
            setup.AddHealthCheckEndpoint("Trading System", "/health");

            // Configure UI settings
            setup.SetHeaderText(config.HealthChecks.UI.PageTitle);
            setup.SetEvaluationTimeInSeconds(config.HealthChecks.IntervalSeconds);
            setup.MaximumHistoryEntriesPerEndpoint(50);

            // Configure UI styling
            if (!string.IsNullOrEmpty(config.HealthChecks.UI.CustomStylesheetUrl))
            {
                setup.AddCustomStylesheet(config.HealthChecks.UI.CustomStylesheetUrl);
            }
        })
        .AddInMemoryStorage();

        // Add monitoring services
        services.AddSingleton<HealthCheckService>();
        services.AddSingleton<HealthCheckStorageService>();
        services.AddSingleton<HealthCheckNotificationService>();
        services.AddHostedService<MonitoringService>();

        // Add Prometheus metrics
        services.AddSingleton<IMetricFactory>(new MetricFactory());

        // Add authentication if enabled
        if (config.HealthChecks.UI.Authentication.Enabled)
        {
            services.AddHealthCheckAuthorization();
            services.AddAuthentication()
                .AddHealthCheckAuth(options =>
                {
                    var authConfig = config.HealthChecks.UI.Authentication;
                    if (authConfig.BasicAuth != null)
                    {
                        options.Username = authConfig.BasicAuth.Username;
                        options.Password = authConfig.BasicAuth.Password;
                    }
                });
        }

        // Add caching
        if (config.HealthChecks.Cache.Enabled)
        {
            if (config.HealthChecks.Cache.UseRedis)
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = config.HealthChecks.Cache.RedisConnectionString;
                });
            }
            else
            {
                services.AddDistributedMemoryCache();
            }
        }

        return services;
    }

    public static IApplicationBuilder UseTradingSystemHealthChecks(
        this IApplicationBuilder app,
        IOptions<MonitoringConfig> config)
    {
        var monitoringConfig = config.Value;

        // Configure health check middleware pipeline
        app.UseHealthCheckPipeline();

        // Use health check middleware components
        if (monitoringConfig.HealthChecks.RateLimit.Enabled)
        {
            app.UseHealthCheckRateLimit();
        }

        if (monitoringConfig.HealthChecks.Cache.Enabled)
        {
            app.UseHealthCheckCaching();
        }

        if (monitoringConfig.HealthChecks.Compression.Enabled)
        {
            app.UseHealthCheckCompression();
        }

        if (monitoringConfig.HealthChecks.Logging.Enabled)
        {
            app.UseHealthCheckLogging();
        }

        // Map health check endpoints
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
            AllowCachingResponses = monitoringConfig.HealthChecks.Cache.Enabled,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
            AllowCachingResponses = false
        });

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
            AllowCachingResponses = false
        });

        // Map health check UI
        app.MapHealthChecksUI(options =>
        {
            options.UIPath = monitoringConfig.HealthChecks.UI.Path;
            options.ApiPath = monitoringConfig.HealthChecks.UI.ApiPath;
            options.UseRelativeApiPath = true;
            options.UseRelativeResourcesPath = true;
            options.AsideMenuOpened = false;
        });

        return app;
    }
}
