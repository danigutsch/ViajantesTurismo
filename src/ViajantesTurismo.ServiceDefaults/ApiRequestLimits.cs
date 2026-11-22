using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using PandaBear.AspNetCore;

namespace ViajantesTurismo.ServiceDefaults;

/// <summary>Request limits shared by the Admin and Catalog API hosts.</summary>
public static class ApiRequestLimits
{
    private const long MaxRequestBodyBytes = 8 * 1024 * 1024;

    /// <summary>The deadline policy for intake, scanning, and processing of a multipart upload.</summary>
    public const string MultipartUploadTimeoutPolicy = "multipart-upload";

    /// <summary>Sets the Kestrel body size backstop and transport timeouts.</summary>
    public static IWebHostBuilder ConfigureApiRequestLimits(this IWebHostBuilder webHost)
    {
        ArgumentNullException.ThrowIfNull(webHost);

        return webHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxRequestBodySize = MaxRequestBodyBytes;
            options.Limits.MinRequestBodyDataRate = new MinDataRate(1_024, TimeSpan.FromSeconds(5));
            options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
            options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(130);
        });
    }

    /// <summary>Registers the timeout policy shared by the multipart upload endpoints.</summary>
    public static IServiceCollection AddMultipartUploadTimeout(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddRequestTimeouts(options =>
            options.AddPolicy(MultipartUploadTimeoutPolicy, TimeSpan.FromMinutes(2)));
    }

    /// <summary>Enables request timeouts and forwards the multipart deadline to implicit form binding.</summary>
    public static IApplicationBuilder UseApiRequestTimeouts(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseRequestTimeouts();
        return app.UseFormBindingDeadline();
    }
}
