using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Http.Resilience;
using PandaBear.AspNetCore;
using Polly;

namespace ViajantesTurismo.Public.Web;

/// <summary>Configures rate limiting and catalog client resilience for Public Web.</summary>
internal static class PublicWebSecurityBaseline
{
    /// <summary>Name of the rate limit policy applied to public read endpoints.</summary>
    internal const string PublicReadRateLimitPolicy = "public-web-read";

    /// <summary>Adds the per-remote-IP fixed-window rate limiter and a no-store rejection response.</summary>
    public static IServiceCollection AddPublicWebSecurityBaseline(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddRateLimiter(options =>
        {
            options.AddRemoteIpFixedWindowPolicy(PublicReadRateLimitPolicy, 120, TimeSpan.FromMinutes(1));
            options.OnRejected = static (context, _) =>
            {
                PublicWebHttpCache.SetNoStore(context.HttpContext);
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds)
                        .ToString(CultureInfo.InvariantCulture);
                }

                return ValueTask.CompletedTask;
            };
        });

        return services;
    }

    /// <summary>Stops retries and circuit-breaker counting for public catalog quota (429) responses.</summary>
    public static void ConfigureCatalogResilience(HttpStandardResilienceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var retryShouldHandle = options.Retry.ShouldHandle;
        options.Retry.ShouldHandle = arguments =>
        {
            if (IsPublicCatalogQuotaResponse(arguments.Context.GetRequestMessage(), arguments.Outcome.Result))
            {
                return ValueTask.FromResult(false);
            }

            return retryShouldHandle(arguments);
        };

        var circuitBreakerShouldHandle = options.CircuitBreaker.ShouldHandle;
        options.CircuitBreaker.ShouldHandle = arguments =>
        {
            if (IsPublicCatalogQuotaResponse(arguments.Context.GetRequestMessage(), arguments.Outcome.Result))
            {
                return ValueTask.FromResult(false);
            }

            return circuitBreakerShouldHandle(arguments);
        };
    }

    private static bool IsPublicCatalogQuotaResponse(HttpRequestMessage? request, HttpResponseMessage? response)
    {
        return response?.StatusCode == HttpStatusCode.TooManyRequests
            && request?.RequestUri is { IsAbsoluteUri: true } uri
            && uri.AbsolutePath.StartsWith(PublicCatalogApiClient.RoutePrefix + "/", StringComparison.OrdinalIgnoreCase);
    }
}
