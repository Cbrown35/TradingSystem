using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using TradingSystem.Core.Configuration;

namespace TradingSystem.Core.Monitoring.Auth;

public class HealthCheckAuthRequirement : IAuthorizationRequirement
{
    public string PolicyName { get; }

    public HealthCheckAuthRequirement(string policyName)
    {
        PolicyName = policyName;
    }
}

public class HealthCheckAuthHandler : AuthorizationHandler<HealthCheckAuthRequirement>
{
    private readonly MonitoringConfig _config;

    public HealthCheckAuthHandler(IOptions<MonitoringConfig> config)
    {
        _config = config.Value;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        HealthCheckAuthRequirement requirement)
    {
        if (!_config.HealthChecks.UI.Authentication.Enabled)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var user = context.User;

        switch (requirement.PolicyName)
        {
            case HealthCheckPolicies.ViewHealthChecks:
                if (user.Identity?.IsAuthenticated == true)
                {
                    context.Succeed(requirement);
                }
                break;

            case HealthCheckPolicies.ManageHealthChecks:
            case HealthCheckPolicies.TestNotifications:
                if (user.IsInRole("HealthCheckAdmin"))
                {
                    context.Succeed(requirement);
                }
                break;
        }

        return Task.CompletedTask;
    }
}

public static class HealthCheckAuthPolicyExtensions
{
    public static void AddHealthCheckPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(HealthCheckPolicies.ViewHealthChecks, policy =>
            policy.Requirements.Add(new HealthCheckAuthRequirement(HealthCheckPolicies.ViewHealthChecks)));

        options.AddPolicy(HealthCheckPolicies.ManageHealthChecks, policy =>
            policy.Requirements.Add(new HealthCheckAuthRequirement(HealthCheckPolicies.ManageHealthChecks)));

        options.AddPolicy(HealthCheckPolicies.TestNotifications, policy =>
            policy.Requirements.Add(new HealthCheckAuthRequirement(HealthCheckPolicies.TestNotifications)));
    }

    public static IServiceCollection AddHealthCheckPolicyServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, HealthCheckAuthHandler>();
        
        services.AddAuthorization(options =>
        {
            options.AddHealthCheckPolicies();
        });

        return services;
    }
}

public static class HealthCheckAuthorizationDefaults
{
    public const string AuthenticationScheme = "HealthCheckAuth";
    public const string AdminRole = "HealthCheckAdmin";
    public const string ViewerRole = "HealthCheckViewer";
}

public class HealthCheckAuthorizationOptions
{
    public bool RequireAuthentication { get; set; } = true;
    public bool RequireHttps { get; set; } = true;
    public string[] AllowedIpAddresses { get; set; } = Array.Empty<string>();
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
    public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromHours(1);
}

public static class HealthCheckAuthorizationOptionsExtensions
{
    public static IServiceCollection AddHealthCheckAuthorization(
        this IServiceCollection services,
        Action<HealthCheckAuthorizationOptions>? configureOptions = null)
    {
        var options = new HealthCheckAuthorizationOptions();
        configureOptions?.Invoke(options);

        services.AddSingleton(Options.Create(options));
        services.AddHealthCheckPolicyServices();

        return services;
    }
}
