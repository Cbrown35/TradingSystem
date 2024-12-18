namespace TradingSystem.Core.Monitoring.Auth;

public static class HealthCheckAuthorizationDefaults
{
    public const string AuthenticationScheme = "HealthCheckAuth";
    public const string AdminRole = "HealthCheckAdmin";
    public const string ViewerRole = "HealthCheckViewer";
    public const string AdminPolicy = "HealthCheckAdminPolicy";
    public const string ViewerPolicy = "HealthCheckViewerPolicy";
    public const string TestNotificationsPolicy = "HealthCheckTestNotificationsPolicy";
}

public static class HealthCheckPolicies
{
    public const string ViewHealthChecks = "ViewHealthChecks";
    public const string ManageHealthChecks = "ManageHealthChecks";
    public const string TestNotifications = "TestNotifications";
}
