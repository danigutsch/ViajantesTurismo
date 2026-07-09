using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SharedKernel.Observability;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.ServiceDefaults;

/// <summary>
/// Provides extension methods for configuring common service defaults, including service discovery,
/// health checks, and OpenTelemetry instrumentation for .NET applications.
/// </summary>
[PublicAPI]
public static class ServiceDefaultsExtensions
{
    /// <summary>
    /// Adds a set of default services and configurations to the host builder, including OpenTelemetry,
    /// health checks, and service discovery.
    /// </summary>
    /// <remarks>
    /// Clients that require HTTPS-only service discovery can configure <c>ServiceDiscoveryOptions.AllowedSchemes</c>
    /// via dependency injection before calling this method.
    /// </remarks>
    /// <typeparam name="TBuilder">The type of the host application builder.</typeparam>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The updated host application builder.</returns>
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureOpenTelemetry();
        builder.AddApplicationVersionLogging();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        return builder;
    }

    /// <summary>
    /// Configures OpenTelemetry logging, metrics, and tracing for the application builder.
    /// </summary>
    /// <remarks>This method adds OpenTelemetry instrumentation for ASP.NET Core requests,
    /// runtime metrics, gRPC client calls, and Entity Framework Core operations. It also configures logging to include
    /// formatted messages and scopes. Health check and aliveness endpoints are excluded from tracing by default.</remarks>
    /// <typeparam name="TBuilder">The type of the application builder to configure. Must implement <see cref="IHostApplicationBuilder"/>.</typeparam>
    /// <param name="builder">The application builder to configure with OpenTelemetry services and instrumentation.</param>
    /// <returns>The same application builder instance, configured with OpenTelemetry logging, metrics, and tracing.</returns>
    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        ObservabilityBuilderExtensions.ConfigureOpenTelemetry(builder);

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddCatalogMetrics()
                    .AddSharedKernelMediatorMetrics()
                    .AddSharedKernelProviderMetrics();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddCatalogTracing()
                    .AddSharedKernelMediatorTracing()
                    .AddSharedKernelProviderTracing()
                    .AddAspNetCoreInstrumentation(options =>
                        options.Filter = context =>
                            !context.Request.Path.StartsWithSegments(EndpointPaths.Health, StringComparison.OrdinalIgnoreCase)
                            && !context.Request.Path.StartsWithSegments(EndpointPaths.Aliveness, StringComparison.OrdinalIgnoreCase)
                    )
                    .AddGrpcClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static void AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }
    }

    private static void AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
    }

    /// <summary>
    /// Configures default health check endpoints for the application.
    /// </summary>
    /// <remarks>
    /// The endpoints return only the health status text by default. They are safe for orchestrator probes and
    /// smoke checks because they do not include exception details or dependency-specific payloads.
    /// </remarks>
    /// <param name="app">The <see cref="WebApplication"/> instance to configure with default endpoints.</param>
    /// <returns>The same <see cref="WebApplication"/> instance, with health check endpoints mapped.</returns>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapHealthChecks(EndpointPaths.Health);

        app.MapHealthChecks(EndpointPaths.Aliveness, new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        return app;
    }
}
