using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ViajantesTurismo.Public.WebTests;

/// <summary>Resolves the options that the Public Web security baseline configures.</summary>
internal static class PublicWebSecurityBaselineTestsHelpers
{
    /// <summary>Registers the baseline and returns the rate limiter options it configures.</summary>
    public static RateLimiterOptions ConfiguredRateLimiterOptions()
    {
        using var provider = new ServiceCollection().AddPublicWebSecurityBaseline().BuildServiceProvider();
        return provider.GetRequiredService<IOptions<RateLimiterOptions>>().Value;
    }
}
