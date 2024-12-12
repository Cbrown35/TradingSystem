using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using TradingSystem.Core.Configuration;
using TradingSystem.Core.Monitoring.Models;

namespace TradingSystem.Core.Monitoring;

public class HealthCheckService : IHealthCheck
{
    private readonly ILogger<HealthCheckService> _logger;
    private readonly MonitoringConfig _config;
    private readonly IServiceProvider _serviceProvider;

    public HealthCheckService(
        ILogger<HealthCheckService> logger,
        IOptions<MonitoringConfig> config,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _config = config.Value;
        _serviceProvider = serviceProvider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var data = new Dictionary<string, object>
            {
                { "LastCheckTime", DateTime.UtcNow },
                { "Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production" }
            };

            // Check core services
            var coreServicesHealthy = await CheckCoreServicesAsync();
            if (!coreServicesHealthy)
            {
                return HealthCheckResult.Unhealthy("Core services check failed", null, data);
            }

            // Check infrastructure
            var infrastructureHealthy = await CheckInfrastructureAsync();
            if (!infrastructureHealthy)
            {
                return HealthCheckResult.Degraded("Infrastructure check failed", null, data);
            }

            // Check dependencies
            var dependenciesHealthy = await CheckDependenciesAsync();
            if (!dependenciesHealthy)
            {
                return HealthCheckResult.Degraded("Dependencies check failed", null, data);
            }

            return HealthCheckResult.Healthy("All systems operational", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing health check");
            return HealthCheckResult.Unhealthy("Health check failed", ex);
        }
    }

    private async Task<bool> CheckCoreServicesAsync()
    {
        try
        {
            // Add core service checks here
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Core services check failed");
            return false;
        }
    }

    private async Task<bool> CheckInfrastructureAsync()
    {
        try
        {
            // Add infrastructure checks here
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Infrastructure check failed");
            return false;
        }
    }

    private async Task<bool> CheckDependenciesAsync()
    {
        try
        {
            // Add dependency checks here
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dependencies check failed");
            return false;
        }
    }

    public IReadOnlyList<HealthCheckEndpoint> GetRegisteredEndpoints()
    {
        // Create a default list of endpoints based on the configuration
        var endpoints = new List<HealthCheckEndpoint>
        {
            new HealthCheckEndpoint
            {
                Name = "Core Services",
                Uri = "/health/core",
                Weight = 3,
                Tags = new List<string> { "core" }
            },
            new HealthCheckEndpoint
            {
                Name = "Infrastructure",
                Uri = "/health/infrastructure",
                Weight = 2,
                Tags = new List<string> { "infrastructure" }
            },
            new HealthCheckEndpoint
            {
                Name = "Dependencies",
                Uri = "/health/dependencies",
                Weight = 1,
                Tags = new List<string> { "dependencies" }
            }
        };

        return endpoints;
    }
}
