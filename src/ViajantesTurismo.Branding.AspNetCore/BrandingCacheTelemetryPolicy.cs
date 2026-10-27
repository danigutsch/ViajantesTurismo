using Microsoft.AspNetCore.OutputCaching;

namespace ViajantesTurismo.Branding.AspNetCore;

/// <summary>Counts public Branding responses served from the output cache.</summary>
internal sealed class BrandingCacheTelemetryPolicy : IOutputCachePolicy
{
    /// <inheritdoc />
    public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken ct) =>
        ValueTask.CompletedTask;

    /// <inheritdoc />
    public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.HttpContext.RequestServices.GetRequiredService<BrandingTelemetry>().CacheHit();
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken ct) =>
        ValueTask.CompletedTask;
}
