using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SharedKernel.Observability;
using SharedKernel.ServiceDefaults;

namespace ViajantesTurismo.ServiceDefaults;

/// <summary>
/// What every ViajantesTurismo host adds to the SharedKernel service defaults:
/// <code>
/// builder.AddApplicationDefaults(ViajantesTurismoServiceDefaults.Configure).AddViajantesTurismoTelemetry();
/// </code>
/// </summary>
[PublicAPI]
public static class ViajantesTurismoServiceDefaults
{
    /// <summary>The name pattern of every ViajantesTurismo meter and activity source.</summary>
    private const string NamePattern = "ViajantesTurismo.*";

    /// <summary>The native Npgsql meter: connection-pool and command metrics.</summary>
    private const string NpgsqlMeterName = "Npgsql";

    /// <summary>Approved dimensions on exported native Npgsql metrics.</summary>
    private static readonly string[] NpgsqlMetricTagKeys =
        ["db.client.connection.pool.name", "db.client.connection.state", "db.system.name"];

    /// <summary>Business attribute names that may carry personal data, compared without case.</summary>
    private static readonly string[] SensitiveAttributeNames =
    [
        "booking.id",
        "booking_id",
        "bookingId",
        "customer.email",
        "customer.id",
        "customer_email",
        "customer_id",
        "customerEmail",
        "customerId",
        "KeyId",
    ];

    /// <summary>Prefixes of the payment and financial attribute names, which must never be exported.</summary>
    private static readonly string[] SensitiveAttributePrefixes =
        ["payment", "financial", "recipient", "fx.quote", "receipt", "refund", "reconciliation", "allocation"];

    /// <summary>
    /// Sets the ViajantesTurismo choices on the SharedKernel service defaults: the business attribute names the privacy processors remove.
    /// The SharedKernel standard resilience handler stays on every <see cref="HttpClient"/>; it never retries unsafe methods such as a
    /// customer creation or a payment instruction.
    /// </summary>
    /// <param name="options">The SharedKernel service-defaults options.</param>
    public static void Configure(ServiceDefaultsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.SensitiveTelemetryAttributes.UnionWith(SensitiveAttributeNames);
    }

    /// <summary>
    /// Adds what the SharedKernel service defaults leave to the application: the payment and financial attribute prefixes, the
    /// ViajantesTurismo meters and activity sources, the Npgsql meter, gRPC client and EF Core tracing and the version log line. It also
    /// keeps OpenTelemetry the only logging provider, so formatted messages never reach the console or another default provider.
    /// </summary>
    /// <typeparam name="TBuilder">The host builder type.</typeparam>
    /// <param name="builder">The host builder, after <c>AddApplicationDefaults</c>.</param>
    /// <returns>The same builder.</returns>
    public static TBuilder AddViajantesTurismoTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Logging.ClearProviders();
        builder.Logging.AddOpenTelemetry();
        builder.AddTelemetryPrivacy(static name => SensitiveAttributePrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));
        builder.AddApplicationVersionLogging();
        builder.Services.AddOpenTelemetry()
            .WithMetrics(static metrics => metrics
                .AddMeter(NamePattern, NpgsqlMeterName)
                .AddView(static instrument => instrument.Meter.Name == NpgsqlMeterName
                    ? new MetricStreamConfiguration { TagKeys = NpgsqlMetricTagKeys }
                    : null))
            .WithTracing(static tracing => tracing
                .AddSource(NamePattern)
                .AddGrpcClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation());

        return builder;
    }
}
