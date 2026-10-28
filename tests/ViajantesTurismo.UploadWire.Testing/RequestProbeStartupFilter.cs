using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;

namespace ViajantesTurismo.UploadWire.Testing;

/// <summary>Adds an outermost middleware that feeds a <see cref="RequestLifetimeProbe"/> for the observed request.</summary>
public sealed class RequestProbeStartupFilter(RequestLifetimeProbe probe) : IStartupFilter
{
    /// <summary>Registers the filter for the probe.</summary>
    public static void Register(IServiceCollection services, RequestLifetimeProbe probe)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IStartupFilter>(new RequestProbeStartupFilter(probe));
    }

    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return app =>
        {
            app.Use(async (context, pipeline) =>
            {
                if (!context.Request.Path.Value!.EndsWith(probe.PathSuffix, StringComparison.Ordinal))
                {
                    await pipeline(context);
                    return;
                }

                probe.OnStarted();
                await using var registration = context.RequestAborted.Register(probe.OnAborted).ConfigureAwait(false);
                context.Request.Body = new ReadObservingStream(context.Request.Body, probe);
                try
                {
                    await pipeline(context);
                }
                finally
                {
                    probe.OnFinished(context.Response.StatusCode);
                }
            });
            next(app);
        };
    }
}
