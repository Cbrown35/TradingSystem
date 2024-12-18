using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
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
                var anonymousClaims = new[] { new Claim(ClaimTypes.Name, "anonymous") };
                var anonymousIdentity = new ClaimsIdentity(anonymousClaims, Scheme.Name);
                var anonymousPrincipal = new ClaimsPrincipal(anonymousIdentity);
                var anonymousTicket = new AuthenticationTicket(anonymousPrincipal, Scheme.Name);
                return Task.FromResult(AuthenticateResult.Success(anonymousTicket));
            }

            if (authConfig.Provider?.ToLower() != "basic")
            {
                return Task.FromResult(AuthenticateResult.Fail("Unsupported authentication provider"));
            }

            if (!Request.Headers.ContainsKey("Authorization"))
            {
                Response.Headers["WWW-Authenticate"] = "Basic";
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

            var authClaims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, HealthCheckAuthorizationDefaults.AdminRole)
            };

            var authIdentity = new ClaimsIdentity(authClaims, Scheme.Name);
            var authPrincipal = new ClaimsPrincipal(authIdentity);
            var authTicket = new AuthenticationTicket(authPrincipal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(authTicket));
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
            HealthCheckAuthorizationDefaults.AuthenticationScheme, null);
    }
}
