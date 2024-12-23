using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TradingSystem.Core.Configuration;
using TradingSystem.Infrastructure;
using TradingSystem.Infrastructure.Data;
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
    serverOptions.Listen(IPAddress.Any, 80);
});

// Add services to the container
builder.Services.AddControllers();

// Register Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Trading System API", Version = "v1" });
});

// Configure monitoring
var monitoringConfig = builder.Configuration
    .GetSection("Monitoring")
    .Get<MonitoringConfig>() ?? new MonitoringConfig();

// Add basic health check
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), new[] { "api" })
    .AddCheck("ready", () => HealthCheckResult.Healthy(), new[] { "ready" });

// Configure health checks UI
builder.Services.AddHealthChecksUI(setup =>
{
    setup.SetEvaluationTimeInSeconds(10);
    setup.MaximumHistoryEntriesPerEndpoint(50);
    setup.SetApiMaxActiveRequests(1);
    
    // Configure health check endpoint using container's service name
    setup.AddHealthCheckEndpoint("Trading System", "http://127.0.0.1:80/healthz");
})
.AddInMemoryStorage();

// Configure HTTP client for health checks
builder.Services.AddHttpClient("HealthChecks", client =>
{
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.BaseAddress = new Uri("http://127.0.0.1:80/");
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    UseProxy = false,
    AllowAutoRedirect = false,
    UseCookies = false,
    ConnectCallback = async (context, cancellationToken) =>
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await socket.ConnectAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 80), cancellationToken);
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

// Map health check endpoints first
app.MapHealthChecks("/healthz", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Configure remaining middleware
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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
        context.Request.Host = new HostString("127.0.0.1", 80);
        context.Request.Scheme = "http";
    }
    await next();
});

// Configure health check middleware
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/healthz"))
    {
        context.Request.Host = new HostString("127.0.0.1", 80);
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
        context.Request.Host = new HostString("127.0.0.1", context.Request.Host.Port ?? 80);
    }
    await next();
});

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
