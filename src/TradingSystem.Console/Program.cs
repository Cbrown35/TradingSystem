using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prometheus;
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

// Configure caching
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();

// Add ASP.NET Core health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<TradingContext>("Database")
    .AddCheck("self", () => HealthCheckResult.Healthy());

// Add health checks UI
builder.Services.AddHealthChecksUI(setup =>
{
    setup.SetEvaluationTimeInSeconds(15);
    setup.MaximumHistoryEntriesPerEndpoint(50);
    setup.AddHealthCheckEndpoint("Trading System", "/healthz");
})
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

// Map health check endpoints with unique paths
app.MapHealthChecks("/healthz", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Map health checks UI with unique paths
app.MapHealthChecksUI(options =>
{
    options.UIPath = "/healthz-ui";
    options.ApiPath = "/healthz-api";
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
