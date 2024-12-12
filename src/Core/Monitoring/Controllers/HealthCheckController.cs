using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;
using TradingSystem.Core.Monitoring.Auth;
using TradingSystem.Core.Monitoring.Models;
using TradingSystem.Core.Monitoring.Services;

namespace TradingSystem.Core.Monitoring.Controllers;

[ApiController]
[Route("api/health")]
[HealthCheckAuth]
public class HealthCheckController : ControllerBase
{
    private readonly ILogger<HealthCheckController> _logger;
    private readonly HealthCheckService _healthCheckService;
    private readonly HealthCheckStorageService _storageService;
    private readonly HealthCheckNotificationService _notificationService;
    private readonly Counter _requestCounter;
    private readonly Histogram _requestDuration;
    private readonly Counter _errorCounter;

    public HealthCheckController(
        ILogger<HealthCheckController> logger,
        HealthCheckService healthCheckService,
        HealthCheckStorageService storageService,
        HealthCheckNotificationService notificationService)
    {
        _logger = logger;
        _healthCheckService = healthCheckService;
        _storageService = storageService;
        _notificationService = notificationService;

        _requestCounter = Metrics.CreateCounter(
            "healthcheck_requests_total",
            "Total number of health check requests",
            new CounterConfiguration
            {
                LabelNames = new[] { "endpoint", "method", "status" }
            });

        _requestDuration = Metrics.CreateHistogram(
            "healthcheck_request_duration_seconds",
            "Duration of health check requests",
            new HistogramConfiguration
            {
                LabelNames = new[] { "endpoint", "method" }
            });

        _errorCounter = Metrics.CreateCounter(
            "healthcheck_errors_total",
            "Total number of health check errors",
            new CounterConfiguration
            {
                LabelNames = new[] { "endpoint", "code", "message" }
            });
    }

    [HttpGet]
    [ProducesResponseType(typeof(HealthCheckResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealthStatus()
    {
        try
        {
            var context = new HealthCheckContext();
            var result = await _healthCheckService.CheckHealthAsync(context);

            _requestCounter.WithLabels("health", "GET", result.Status.ToString()).Inc();

            using (_requestDuration.WithLabels("health", "GET").NewTimer())
            {
                return result.Status == HealthStatus.Healthy ? 
                    Ok(result) : 
                    StatusCode(StatusCodes.Status503ServiceUnavailable, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking health status");
            _errorCounter.WithLabels("health", "500", ex.Message).Inc();

            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                Status = "Error",
                Message = "Failed to check health status",
                Error = ex.Message
            });
        }
    }

    [HttpGet("history")]
    [ProducesResponseType(typeof(IEnumerable<HealthCheckResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHealthHistory(
        [FromQuery] string endpoint,
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null,
        [FromQuery] int? limit = null)
    {
        try
        {
            var history = await _storageService.GetHistory(endpoint, startTime, endTime, limit);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving health history");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                Status = "Error",
                Message = "Failed to retrieve health history",
                Error = ex.Message
            });
        }
    }

    [HttpGet("analytics")]
    [ProducesResponseType(typeof(HealthCheckAnalytics), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHealthAnalytics(
        [FromQuery] string endpoint,
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null)
    {
        try
        {
            var analytics = await _storageService.GetAnalytics(
                endpoint,
                startTime ?? DateTime.UtcNow.AddDays(-7),
                endTime ?? DateTime.UtcNow);
            
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving health analytics");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                Status = "Error",
                Message = "Failed to retrieve health analytics",
                Error = ex.Message
            });
        }
    }

    [HttpGet("issues")]
    [ProducesResponseType(typeof(List<HealthCheckIssue>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopIssues(
        [FromQuery] string endpoint,
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null,
        [FromQuery] int limit = 10)
    {
        try
        {
            var analytics = await _storageService.GetAnalytics(
                endpoint,
                startTime ?? DateTime.UtcNow.AddDays(-7),
                endTime ?? DateTime.UtcNow);
            
            return Ok(analytics.Issues.Take(limit));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving health issues");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                Status = "Error",
                Message = "Failed to retrieve health issues",
                Error = ex.Message
            });
        }
    }

    [HttpGet("endpoints")]
    [ProducesResponseType(typeof(List<HealthCheckEndpoint>), StatusCodes.Status200OK)]
    public IActionResult GetEndpoints()
    {
        try
        {
            var endpoints = _healthCheckService.GetRegisteredEndpoints();
            return Ok(endpoints);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving health check endpoints");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                Status = "Error",
                Message = "Failed to retrieve health check endpoints",
                Error = ex.Message
            });
        }
    }

    [HttpPost("test-notification")]
    [Authorize(Policy = HealthCheckPolicies.TestNotifications)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> TestNotification(
        [FromQuery] string channel,
        [FromBody] HealthCheckResult testResult)
    {
        try
        {
            var notification = new NotificationChannelConfig
            {
                Name = channel,
                Type = channel.ToLowerInvariant(),
                Settings = new Dictionary<string, string>(),
                Filter = new NotificationFilterConfig
                {
                    IncludeTags = new List<string>(),
                    MinimumStatus = new List<string> { "Unhealthy" }
                }
            };

            await _notificationService.SendNotification(notification, testResult);
            return Ok(new { Message = "Test notification sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test notification");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                Status = "Error",
                Message = "Failed to send test notification",
                Error = ex.Message
            });
        }
    }
}
