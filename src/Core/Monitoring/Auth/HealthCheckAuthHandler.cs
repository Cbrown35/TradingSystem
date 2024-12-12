using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using TradingSystem.Core.Configuration;

namespace TradingSystem.Core.Monitoring.Auth;

public class HealthCheckAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IOptionsMonitor<MonitoringConfig> _config;

    public HealthCheckAuthHandler(
        IOptionsMonitor<MonitoringConfig> config,
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
        _config = config;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            var authConfig = _config.CurrentValue.HealthChecks.UI.Authentication;

            if (!authConfig.Enabled)
            {
                var claims = new[] { new Claim(ClaimTypes.Name, "anonymous") };
                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);
                return Task.FromResult(AuthenticateResult.Success(ticket));
            }

            if (authConfig.Provider?.ToLower() != "basic")
            {
                return Task.FromResult(AuthenticateResult.Fail("Unsupported authentication provider"));
            }

            if (!Request.Headers.ContainsKey("Authorization"))
            {
                Response.Headers.Add("WWW-Authenticate", "Basic");
                return Task.FromResult(AuthenticateResult.Fail("Missing authorization header"));
            }

            var authHeader = Request.Headers["Authorization"].ToString();
            if (!authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid authorization header"));
            }

            var credentials = System.Text.Encoding.UTF8
                .GetString(Convert.FromBase64String(authHeader.Substring(6)))
                .Split(':');

            if (credentials.Length != 2)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid credentials format"));
            }

            var username = credentials[0];
            var password = credentials[1];

            if (username != authConfig.BasicAuth?.Username || 
                password != authConfig.BasicAuth?.Password)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid credentials"));
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, "HealthCheckAdmin")
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error authenticating health check request");
            return Task.FromResult(AuthenticateResult.Fail("Authentication failed"));
        }
    }
}

public static class HealthCheckAuthExtensions
{
    public static AuthenticationBuilder AddHealthCheckAuth(this AuthenticationBuilder builder)
    {
        return builder.AddScheme<AuthenticationSchemeOptions, HealthCheckAuthHandler>(
            "HealthCheckAuth", null);
    }
}

public static class HealthCheckPolicies
{
    public const string ViewHealthChecks = "ViewHealthChecks";
    public const string ManageHealthChecks = "ManageHealthChecks";
    public const string TestNotifications = "TestNotifications";
}

public static class HealthCheckAuthorizationExtensions
{
    public static IServiceCollection AddHealthCheckAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(HealthCheckPolicies.ViewHealthChecks, policy =>
                policy.RequireAuthenticatedUser());

            options.AddPolicy(HealthCheckPolicies.ManageHealthChecks, policy =>
                policy.RequireRole("HealthCheckAdmin"));

            options.AddPolicy(HealthCheckPolicies.TestNotifications, policy =>
                policy.RequireRole("HealthCheckAdmin"));
        });

        return services;
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class HealthCheckAuthAttribute : Attribute, Microsoft.AspNetCore.Authorization.IAuthorizeData
{
    public string? AuthenticationSchemes { get; set; } = "HealthCheckAuth";
    public string? Policy { get; set; }
    public string? Roles { get; set; }
}
