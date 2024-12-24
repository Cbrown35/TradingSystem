using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TradingSystem.Core.Configuration;
using TradingSystem.Infrastructure;
using TradingSystem.Infrastructure.Data;
using TradingSystem.Infrastructure.Repositories;
using TradingSystem.Common.Interfaces;
using TradingSystem.RealTrading.Services;
using TradingSystem.RealTrading.Configuration;
using TradingSystem.Console.Services;
using HealthChecks.UI.Client;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.IO;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Listen(IPAddress.Any, 3000);
});

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddAuthentication();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("HealthCheckViewerPolicy", policy =>
        policy.RequireAssertion(_ => true)); // Allow all in development
});

// Configure trading environment
var tradingConfig = builder.Configuration.GetSection("Trading").Get<TradingEnvironmentConfig>() ?? new TradingEnvironmentConfig();

// Add database context with in-memory provider
builder.Services.AddDbContext<TradingContext>(options =>
{
    options.UseInMemoryDatabase("TradingDb")
           .EnableSensitiveDataLogging() // For development
           .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});

// Add memory cache
builder.Services.AddMemoryCache();

// Add trading services
// First, register repositories
builder.Services.AddScoped<ITradeRepository, TradeRepository>();
builder.Services.AddScoped<IMarketDataRepository, MarketDataRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Then, register core services
builder.Services.AddSingleton<IRiskValidationService, RiskValidationService>();
builder.Services.AddSingleton<IExchangeAdapter, SimulatedExchangeAdapter>();

// Configure and register market data cache service
builder.Services.Configure<MarketDataCacheConfig>(options => {
    options.EnableCaching = true;
    options.LatestDataCacheDuration = TimeSpan.FromSeconds(5);
    options.HistoricalDataCacheDuration = TimeSpan.FromMinutes(5);
    options.MaxCacheItems = 1000;
});
builder.Services.AddSingleton<IMarketDataCacheService, MarketDataCacheService>();

// Finally, register dependent services
builder.Services.AddScoped<IMarketDataService, MarketDataService>();
builder.Services.AddScoped<IRiskManager, RiskManager>();
builder.Services.AddScoped<ITradingService, TradingService>();


// Register configuration
builder.Services.AddSingleton(tradingConfig);

// Register Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Trading System API", 
        Version = "v1",
        Description = "API for simulated trading system"
    });
    
    // Group endpoints by controller
    c.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
    c.DocInclusionPredicate((docName, description) => true);
});

// Configure CORS before other middleware
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Configure monitoring
var monitoringConfig = builder.Configuration
    .GetSection("Monitoring")
    .Get<MonitoringConfig>() ?? new MonitoringConfig();

builder.Services.Configure<MonitoringConfig>(options =>
{
    options.HealthChecks.Enabled = true;
    options.HealthChecks.IntervalSeconds = 30;
    options.HealthChecks.UI.Path = "/healthchecks";
    options.HealthChecks.UI.ApiPath = "/healthchecks-api";
});

// Add basic health check
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), new[] { "api" })
    .AddCheck("ready", () => HealthCheckResult.Healthy(), new[] { "ready" });

// Register health check services
builder.Services.AddScoped<TradingSystem.Core.Monitoring.Interfaces.IHealthCheckService, TradingSystem.Core.Monitoring.HealthCheckService>();
builder.Services.AddScoped<TradingSystem.Core.Monitoring.Services.HealthCheckStorageService>();

// Configure health checks UI
builder.Services.AddHealthChecksUI(setup =>
{
    setup.SetEvaluationTimeInSeconds(10);
    setup.MaximumHistoryEntriesPerEndpoint(50);
    setup.SetApiMaxActiveRequests(1);
    
    // Configure health check endpoint using container's service name
    setup.AddHealthCheckEndpoint("Trading System", "http://127.0.0.1:3000/healthz");
})
.AddInMemoryStorage();

// Configure HTTP client for health checks
builder.Services.AddHttpClient("HealthChecks", client =>
{
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.BaseAddress = new Uri("http://127.0.0.1:3000/");
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    UseProxy = false,
    AllowAutoRedirect = false,
    UseCookies = false,
    ConnectCallback = async (context, cancellationToken) =>
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await socket.ConnectAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 3000), cancellationToken);
        return new NetworkStream(socket, true);
    }
});

// Configure forwarded headers
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure middleware pipeline
app.UseForwardedHeaders();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Map health check endpoints first
app.MapHealthChecks("/healthz", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).RequireAuthorization("HealthCheckViewerPolicy");

app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Configure middleware
app.UseCors();

// Always enable Swagger in this test environment
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Trading System API V1");
    c.RoutePrefix = "swagger";
});

// Map health checks UI and remaining endpoints
app.UseEndpoints(endpoints =>
{
    endpoints.MapHealthChecksUI(options =>
    {
        options.UIPath = "/";
        options.ResourcesPath = "/ui/resources";
        options.WebhookPath = "/ui/webhooks";
        options.ApiPath = "/healthchecks-api";
        options.UseRelativeApiPath = true;
        options.UseRelativeResourcesPath = true;
        options.UseRelativeWebhookPath = true;
    });

    endpoints.MapControllers();
});

// Configure request transformation middleware
app.Use(async (context, next) =>
{
    if (context.Request.Host.Host == "::" || context.Request.Host.Host == "[::]")
    {
        context.Request.Host = new HostString("127.0.0.1", 3000);
        context.Request.Scheme = "http";
    }
    await next();
});

// Configure health check middleware
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/healthz"))
    {
        context.Request.Host = new HostString("127.0.0.1", 3000);
        context.Request.Scheme = "http";
    }
    await next();
});

// Configure host name resolution
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
ServicePointManager.DnsRefreshTimeout = 0;

// Configure DNS resolution for localhost
app.Use(async (context, next) =>
{
    if (context.Request.Host.Host == "localhost")
    {
        context.Request.Host = new HostString("127.0.0.1", context.Request.Host.Port ?? 3000);
    }
    await next();
});

try
{
    // Initialize exchange adapter
    var exchangeAdapter = app.Services.GetRequiredService<IExchangeAdapter>();
    foreach (var symbol in tradingConfig.Exchange?.Symbols ?? new[] { "BTCUSD" })
    {
        exchangeAdapter.SubscribeToSymbol(symbol);
    }

    app.Run();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogCritical(ex, "Application terminated unexpectedly");
    throw;
}
