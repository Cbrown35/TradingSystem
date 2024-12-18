using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingSystem.Core.Configuration;
using TradingSystem.Core.Monitoring.Interfaces;
using TradingSystem.Core.Monitoring.Models;
using TradingSystem.Core.Monitoring.Services;

namespace TradingSystem.Core.Monitoring;

public class MonitoringService : IHostedService, IDisposable
{
    private readonly ILogger<MonitoringService> _logger;
    private readonly TradingSystem.Core.Configuration.MonitoringConfig _config;
    private readonly IHealthCheckService _healthCheckService;
    private readonly HealthCheckStorageService _storageService;
    private readonly HealthCheckNotificationService _notificationService;
    private Timer? _monitoringTimer;

    public MonitoringService(
        ILogger<MonitoringService> logger,
        IOptions<TradingSystem.Core.Configuration.MonitoringConfig> config,
        IHealthCheckService healthCheckService,
        HealthCheckStorageService storageService,
        HealthCheckNotificationService notificationService)
    {
        _logger = logger;
        _config = config.Value;
        _healthCheckService = healthCheckService;
        _storageService = storageService;
        _notificationService = notificationService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Monitoring Service is starting");

        _monitoringTimer = new Timer(
            MonitorHealthAsync,
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(_config.HealthChecks.IntervalSeconds));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Monitoring Service is stopping");

        _monitoringTimer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private async void MonitorHealthAsync(object? state)
    {
        try
        {
            var result = await _healthCheckService.CheckHealthAsync();

            // Store the result
            await _storageService.StoreHealthCheckResultAsync(result);

            // Send notifications if needed
            if (result.Status != Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy)
            {
                await _notificationService.ProcessHealthCheckResultAsync(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring health status");
        }
    }

    public void Dispose()
    {
        _monitoringTimer?.Dispose();
    }
}
