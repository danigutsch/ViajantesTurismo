using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace ViajantesTurismo.ServiceDefaults;

/// <summary>
/// Provides extension methods for configuring common service defaults, including service discovery, resilience,
/// health checks, and OpenTelemetry instrumentation for .NET applications.
/// </summary>
[PublicAPI]
public static class ServiceDefaultsExtensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

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

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();

            http.AddServiceDiscovery();
        });

        return builder;
    }

    /// <summary>
    /// Configures OpenTelemetry logging, metrics, and tracing for the application builder, enabling instrumentation for
    /// ASP.NET Core, HTTP clients, and runtime metrics.
    /// </summary>
    /// <remarks>This method adds OpenTelemetry instrumentation for ASP.NET Core requests, HTTP client calls,
    /// and runtime metrics. It also configures logging to include formatted messages and scopes. Health check and
    /// aliveness endpoints are excluded from tracing by default. To enable gRPC instrumentation, ensure the required
    /// package is referenced and uncomment the relevant line in the configuration.</remarks>
    /// <typeparam name="TBuilder">The type of the application builder to configure. Must implement <see cref="IHostApplicationBuilder"/>.</typeparam>
    /// <param name="builder">The application builder to configure with OpenTelemetry services and instrumentation.</param>
    /// <returns>The same application builder instance, configured with OpenTelemetry logging, metrics, and tracing.</returns>
    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(options =>
                        options.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath, StringComparison.OrdinalIgnoreCase)
                            && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath, StringComparison.OrdinalIgnoreCase)
                    )
                    .AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    private static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    /// <summary>
    /// Configures default health check endpoints for the application when running in the development environment.
    /// </summary>
    /// <remarks>
    /// Exposing health check endpoints in production environments should be evaluated carefully because it can
    /// disclose infrastructure details. See https://aka.ms/dotnet/aspire/healthchecks for guidance.
    /// </remarks>
    /// <param name="app">The <see cref="WebApplication"/> instance to configure with default endpoints.</param>
    /// <returns>The same <see cref="WebApplication"/> instance, with health check endpoints mapped if the environment is
    /// development.</returns>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (app.Environment.IsDevelopment())
        {
            app.MapHealthChecks(HealthEndpointPath);

            app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }
}
