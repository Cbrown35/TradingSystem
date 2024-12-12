using Microsoft.Extensions.Options;
using TradingSystem.Core.Configuration;
using TradingSystem.Core.Monitoring.Services;

namespace TradingSystem.Core.Monitoring;

public class MonitoringService : IHostedService, IDisposable
{
    private readonly ILogger<MonitoringService> _logger;
    private readonly MonitoringConfig _config;
    private readonly HealthCheckService _healthCheckService;
    private readonly HealthCheckStorageService _storageService;
    private readonly HealthCheckNotificationService _notificationService;
    private Timer? _monitoringTimer;

    public MonitoringService(
        ILogger<MonitoringService> logger,
        IOptions<MonitoringConfig> config,
        HealthCheckService healthCheckService,
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
            var context = new HealthCheckContext();
            var result = await _healthCheckService.CheckHealthAsync(context);

            // Store the result
            await _storageService.StoreResult("system", new HealthCheckResult
            {
                Status = result.Status.ToString(),
                Duration = TimeSpan.Zero, // Set actual duration
                Timestamp = DateTime.UtcNow,
                Entries = result.Data.Select(d => new HealthCheckEntry
                {
                    Name = d.Key,
                    Status = "Healthy", // Set actual status
                    Description = d.Value?.ToString() ?? string.Empty
                }).ToList()
            });

            // Send notifications if needed
            if (result.Status != HealthStatus.Healthy)
            {
                await _notificationService.SendNotifications(new HealthCheckResult
                {
                    Status = result.Status.ToString(),
                    Duration = TimeSpan.Zero, // Set actual duration
                    Timestamp = DateTime.UtcNow,
                    Entries = result.Data.Select(d => new HealthCheckEntry
                    {
                        Name = d.Key,
                        Status = "Healthy", // Set actual status
                        Description = d.Value?.ToString() ?? string.Empty
                    }).ToList()
                });
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
