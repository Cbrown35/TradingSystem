using Microsoft.AspNetCore.Authorization;

namespace TradingSystem.Core.Monitoring.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class HealthCheckAuthAttribute : AuthorizeAttribute
{
    public HealthCheckAuthAttribute() : base(HealthCheckAuthorizationDefaults.ViewerPolicy)
    {
    }

    public HealthCheckAuthAttribute(string policy) : base(policy)
    {
    }
}
