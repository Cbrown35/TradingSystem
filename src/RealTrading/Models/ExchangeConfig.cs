namespace TradingSystem.RealTrading.Models;

public class TradingViewConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string WebhookEndpoint { get; set; } = string.Empty;
    public int WebhookPort { get; set; } = 5000;
    public string[] AllowedIps { get; set; } = Array.Empty<string>();
}

public class TradovateConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string WebSocketUrl { get; set; } = string.Empty;
    public string RestApiUrl { get; set; } = string.Empty;
    public bool UseDemoAccount { get; set; } = true;
}
