using System.IO.Compression;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using TradingSystem.Core.Configuration;

namespace TradingSystem.Core.Monitoring.Middleware;

public class HealthCheckCompressionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HealthCheckCompressionMiddleware> _logger;
    private readonly MonitoringConfig _config;

    public HealthCheckCompressionMiddleware(
        RequestDelegate next,
        ILogger<HealthCheckCompressionMiddleware> logger,
        IOptions<MonitoringConfig> config)
    {
        _next = next;
        _logger = logger;
        _config = config.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldCompressResponse(context))
        {
            await _next(context);
            return;
        }

        var acceptEncoding = context.Request.Headers.AcceptEncoding.ToString().ToLower();
        var compressionType = GetCompressionType(acceptEncoding);

        if (compressionType == CompressionType.None)
        {
            await _next(context);
            return;
        }

        var originalBody = context.Response.Body;
        using var compressionStream = CreateCompressionStream(compressionType, originalBody);
        context.Response.Body = compressionStream;
        context.Response.Headers.Add("Content-Encoding", GetEncodingName(compressionType));
        context.Response.Headers.Add("Vary", "Accept-Encoding");

        try
        {
            await _next(context);
            if (compressionStream is MemoryStream ms)
            {
                ms.Seek(0, SeekOrigin.Begin);
                await ms.CopyToAsync(originalBody);
            }
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    private bool ShouldCompressResponse(HttpContext context)
    {
        if (!IsHealthCheckEndpoint(context))
            return false;

        if (!_config.HealthChecks.Compression.Enabled)
            return false;

        if (context.Response.Headers.ContainsKey("Content-Encoding"))
            return false;

        var contentType = context.Response.ContentType?.ToLower() ?? "";
        return IsCompressibleContentType(contentType);
    }

    private bool IsHealthCheckEndpoint(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        return path.StartsWith("/health") || 
               path.StartsWith("/metrics") || 
               path.StartsWith("/healthchecks-ui");
    }

    private bool IsCompressibleContentType(string contentType)
    {
        var compressibleTypes = new[]
        {
            "text/",
            "application/json",
            "application/xml",
            "application/javascript",
            "application/x-javascript",
            "application/ecmascript",
            "application/x-ecmascript"
        };

        return compressibleTypes.Any(type => contentType.StartsWith(type));
    }

    private CompressionType GetCompressionType(string acceptEncoding)
    {
        if (string.IsNullOrEmpty(acceptEncoding))
            return CompressionType.None;

        if (_config.HealthChecks.Compression.PreferBrotli && 
            acceptEncoding.Contains("br"))
            return CompressionType.Brotli;

        if (acceptEncoding.Contains("gzip"))
            return CompressionType.Gzip;

        if (acceptEncoding.Contains("deflate"))
            return CompressionType.Deflate;

        return CompressionType.None;
    }

    private Stream CreateCompressionStream(CompressionType compressionType, Stream outputStream)
    {
        switch (compressionType)
        {
            case CompressionType.Gzip:
                return new GZipStream(outputStream, GetCompressionLevel());

            case CompressionType.Deflate:
                return new DeflateStream(outputStream, GetCompressionLevel());

            case CompressionType.Brotli:
                return new BrotliStream(outputStream, GetCompressionLevel());

            default:
                return outputStream;
        }
    }

    private CompressionLevel GetCompressionLevel()
    {
        return _config.HealthChecks.Compression.Level switch
        {
            "fastest" => CompressionLevel.Fastest,
            "optimal" => CompressionLevel.Optimal,
            "smallest" => CompressionLevel.SmallestSize,
            _ => CompressionLevel.Optimal
        };
    }

    private string GetEncodingName(CompressionType compressionType)
    {
        return compressionType switch
        {
            CompressionType.Gzip => "gzip",
            CompressionType.Deflate => "deflate",
            CompressionType.Brotli => "br",
            _ => "identity"
        };
    }
}

public enum CompressionType
{
    None,
    Gzip,
    Deflate,
    Brotli
}

public static class HealthCheckCompressionExtensions
{
    public static IApplicationBuilder UseHealthCheckCompression(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<HealthCheckCompressionMiddleware>();
    }

    public static IServiceCollection AddHealthCheckCompression(
        this IServiceCollection services,
        Action<HealthCheckCompressionOptions>? configureOptions = null)
    {
        var options = new HealthCheckCompressionOptions();
        configureOptions?.Invoke(options);

        services.Configure<HealthCheckCompressionOptions>(opt =>
        {
            opt.Enabled = options.Enabled;
            opt.Level = options.Level;
            opt.MinimumSizeBytes = options.MinimumSizeBytes;
            opt.PreferBrotli = options.PreferBrotli;
            opt.ExcludedPaths = options.ExcludedPaths;
            opt.ExcludedContentTypes = options.ExcludedContentTypes;
        });

        return services;
    }
}

public class HealthCheckCompressionOptions
{
    public bool Enabled { get; set; } = true;
    public string Level { get; set; } = "optimal";
    public int MinimumSizeBytes { get; set; } = 1024;
    public bool PreferBrotli { get; set; } = true;
    public List<string> ExcludedPaths { get; set; } = new();
    public List<string> ExcludedContentTypes { get; set; } = new();
}
