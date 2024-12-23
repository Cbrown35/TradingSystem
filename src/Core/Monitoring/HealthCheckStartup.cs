using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Prometheus;
using TradingSystem.Core.Configuration;
using TradingSystem.Core.Monitoring.Auth;
using TradingSystem.Core.Monitoring.HealthChecks;
using TradingSystem.Core.Monitoring.Interfaces;
using TradingSystem.Core.Monitoring.Middleware;
using TradingSystem.Core.Monitoring.Models;
using TradingSystem.Core.Monitoring.Services;

namespace TradingSystem.Core.Monitoring;

public static class HealthCheckStartup
{
    public static IServiceCollection AddHealthChecking(
        this IServiceCollection services,
        Action<TradingSystem.Core.Configuration.MonitoringConfig> configureOptions)
    {
        services.Configure(configureOptions);

        // Add core health check services
        services.AddSingleton<IHealthCheckService, HealthCheckService>();
        services.AddSingleton<HealthCheckStorageService>();
        services.AddSingleton<HealthCheckNotificationService>();

        // Add component health checks
        services.AddSingleton<BacktesterHealthCheck>();
        services.AddSingleton<MarketDataHealthCheck>();
        services.AddSingleton<RiskManagementHealthCheck>();
        services.AddSingleton<StrategyExecutionHealthCheck>();

        // Add ASP.NET Core health checks
        services.AddHealthChecks()
            .AddCheck<BacktesterHealthCheck>("backtester")
            .AddCheck<MarketDataHealthCheck>("market-data")
            .AddCheck<RiskManagementHealthCheck>("risk-management")
            .AddCheck<StrategyExecutionHealthCheck>("strategy-execution");

        // Add health check UI
        services.AddHealthChecksUI(setup =>
        {
            setup.SetEvaluationTimeInSeconds(15);
            setup.MaximumHistoryEntriesPerEndpoint(50);
            setup.SetApiMaxActiveRequests(3);
            setup.DisableDatabaseMigrations();
            setup.AddHealthCheckEndpoint("Trading System", "http://tradingsystem/health");
            setup.AddHealthCheckEndpoint("Market Data", "http://tradingsystem/health/market-data");
            setup.AddHealthCheckEndpoint("Strategy Execution", "http://tradingsystem/health/strategy");
            setup.AddHealthCheckEndpoint("Risk Management", "http://tradingsystem/health/risk");
            setup.AddHealthCheckEndpoint("Database", "http://postgres/health");
            setup.AddHealthCheckEndpoint("Redis Cache", "http://redis/health");
        })
        .AddInMemoryStorage();

        // Add Prometheus metrics
        var metricConfig = new PrometheusConfiguration
        {
            Enabled = true,
            PrefixName = "tradingsystem",
            DefaultLabels = new Dictionary<string, string>
            {
                ["app"] = "tradingsystem",
                ["component"] = "healthcheck"
            }
        };

        services.AddSingleton(metricConfig);
        services.AddSingleton<IMetricFactory>(sp =>
        {
            var config = sp.GetRequiredService<PrometheusConfiguration>();
            var registry = Metrics.NewCustomRegistry();
            return Metrics.WithCustomRegistry(registry);
        });

        // Add authentication if enabled
        var config = services.BuildServiceProvider()
            .GetRequiredService<IOptions<TradingSystem.Core.Configuration.MonitoringConfig>>()
            .Value;

        if (config.HealthChecks.UI.Authentication.Enabled)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = HealthCheckAuthorizationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = HealthCheckAuthorizationDefaults.AuthenticationScheme;
            })
            .AddHealthCheckAuth();

            services.AddAuthorization(options =>
            {
                options.AddPolicy(HealthCheckPolicies.ViewHealthChecks, policy =>
                    policy.RequireAuthenticatedUser());
                options.AddPolicy(HealthCheckPolicies.TestNotifications, policy =>
                    policy.RequireRole(HealthCheckAuthorizationDefaults.AdminRole));
            });
        }

        return services;
    }

    public static IApplicationBuilder UseHealthChecking(this IApplicationBuilder app)
    {
        // Add middleware pipeline
        app.UseMiddleware<HealthCheckMiddleware>();
        app.UseMiddleware<HealthCheckLoggingMiddleware>();
        app.UseMiddleware<HealthCheckRateLimitMiddleware>();
        app.UseMiddleware<HealthCheckCompressionMiddleware>();
        app.UseMiddleware<HealthCheckCachingMiddleware>();

        // Map health check endpoints
        var endpointRouteBuilder = app as IEndpointRouteBuilder;
        if (endpointRouteBuilder != null)
        {
            // Map main health check endpoint
            endpointRouteBuilder.MapHealthChecks("/health", new HealthCheckOptions
            {
                Predicate = _ => true,
                AllowCachingResponses = false
            });

            // Map component-specific health check endpoints
            endpointRouteBuilder.MapHealthChecks("/health/market-data", new HealthCheckOptions
            {
                Predicate = check => check.Name == "market-data",
                AllowCachingResponses = false
            });

            endpointRouteBuilder.MapHealthChecks("/health/strategy", new HealthCheckOptions
            {
                Predicate = check => check.Name == "strategy-execution",
                AllowCachingResponses = false
            });

            endpointRouteBuilder.MapHealthChecks("/health/risk", new HealthCheckOptions
            {
                Predicate = check => check.Name == "risk-management",
                AllowCachingResponses = false
            });

            endpointRouteBuilder.MapHealthChecks("/health/database", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("database"),
                AllowCachingResponses = false
            });

            endpointRouteBuilder.MapHealthChecks("/health/redis", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("redis"),
                AllowCachingResponses = false
            });

            // Map health check UI
            endpointRouteBuilder.MapHealthChecksUI(options =>
            {
                options.UIPath = "/healthchecks-ui";
                options.ApiPath = "/healthchecks-api";
                options.AddCustomStylesheet("wwwroot/css/healthchecks.css");
            });
        }

        return app;
    }
}

public class PrometheusConfiguration
{
    public bool Enabled { get; set; }
    public string PrefixName { get; set; } = string.Empty;
    public Dictionary<string, string> DefaultLabels { get; set; } = new();
}

public class PrometheusMetricsOptions
{
    public string Prefix { get; set; } = string.Empty;
    public Dictionary<string, string> Labels { get; set; } = new();
}
