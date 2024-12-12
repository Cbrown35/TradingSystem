using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;
using TradingSystem.Core.Monitoring;
using TradingSystem.Core.Monitoring.Auth;
using TradingSystem.Core.Monitoring.Middleware;
using TradingSystem.Core.Configuration;
using TradingSystem.Infrastructure;

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
builder.Services.AddTradingSystemHealthChecks(monitoringConfig);

// Add infrastructure services
builder.Services.AddInfrastructureServices(builder.Configuration);

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

// Use health check pipeline
app.UseTradingSystemHealthChecks(
    app.Services.GetRequiredService<IOptions<MonitoringConfig>>());

// Enable prometheus metrics
app.UseMetricServer();
app.UseHttpMetrics(options =>
{
    options.AddCustomLabel("app", "trading_system");
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
