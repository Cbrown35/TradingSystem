using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using TradingSystem.Core.Monitoring;
using TradingSystem.Core.Monitoring.Auth;
using TradingSystem.Core.Monitoring.Middleware;
using TradingSystem.Core.Configuration;
using TradingSystem.Infrastructure;
using TradingSystem.Infrastructure.Data;
using HealthChecks.UI.Client;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure monitoring
var monitoringConfig = builder.Configuration
    .GetSection("Monitoring")
    .Get<MonitoringConfig>() ?? new MonitoringConfig();

builder.Services.Configure<MonitoringConfig>(
    builder.Configuration.GetSection("Monitoring"));

// Add health checks
builder.Services.AddHealthChecking(options =>
{
    options.HealthChecks = monitoringConfig.HealthChecks;
});

// Add health checks UI
builder.Services.AddHealthChecksUI()
    .AddInMemoryStorage();

// Add infrastructure services
builder.Services.AddDbContext<TradingContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Add HTTP client factory
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Configure authentication and authorization
if (monitoringConfig.HealthChecks.UI.Authentication.Enabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

// Define health check response writer
static Task HealthCheckResponseWriter(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    return UIResponseWriter.WriteHealthCheckUIResponse(context, report);
}

// Map health check endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    AllowCachingResponses = false,
    ResponseWriter = HealthCheckResponseWriter
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    AllowCachingResponses = false,
    ResponseWriter = HealthCheckResponseWriter
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => !check.Tags.Contains("ready"),
    AllowCachingResponses = false,
    ResponseWriter = HealthCheckResponseWriter
});

// Map health checks UI
app.UseHealthChecksUI(config =>
{
    config.UIPath = "/healthchecks-ui";
    config.ApiPath = "/healthchecks-api";
});

// Enable prometheus metrics
app.UseMetricServer();
app.UseHttpMetrics(options => 
{
    options.AddCustomLabel("app", _ => "trading_system");
});

app.MapControllers();

try
{
    app.Run();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogCritical(ex, "Application terminated unexpectedly");
    throw;
}
