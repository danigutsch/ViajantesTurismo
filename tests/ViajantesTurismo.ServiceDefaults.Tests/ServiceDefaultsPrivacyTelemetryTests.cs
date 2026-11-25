using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using SharedKernel.Testing.Logging;

namespace ViajantesTurismo.ServiceDefaults.Tests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, SharedKernel.Testing.TestTraitValues.SecurityCategory)]
public sealed class ServiceDefaultsPrivacyTelemetryTests
{
    [Fact]
    public void Host_defaults_remove_preconfigured_non_otlp_logging_providers()
    {
        // Arrange
        using var recordingProvider = new RecordingLoggerProvider();
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = Environments.Production });
        builder.Logging.AddProvider(recordingProvider);
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = string.Empty;

        // Act
        HostDefaults.Add(builder);
        using var host = builder.Build();
        var logger = host.Services.GetRequiredService<ILogger<ServiceDefaultsPrivacyTelemetryTests>>();
        PrivacyTestLogger.LogFailure(
            logger,
            new InvalidOperationException("traveler@example.com at /customers/private"),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "traveler@example.com",
            "customers/private/media/passport.jpg",
            "conflict");

        // Assert
        recordingProvider.Records.ShouldBeEmpty();
    }

    [Fact]
    public void Host_defaults_keep_exporting_logs_through_open_telemetry()
    {
        // Arrange
        var exportedLogs = new ConcurrentQueue<CapturedLogRecord>();
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = string.Empty;
        HostDefaults.Add(builder);
        builder.Logging.AddOpenTelemetry(logging =>
            logging.AddProcessor(_ => new SimpleLogRecordExportProcessor(new CollectingLogRecordExporter(exportedLogs))));
        using var host = builder.Build();
        var logger = host.Services.GetRequiredService<ILogger<ServiceDefaultsPrivacyTelemetryTests>>();

        // Act
        PrivacyTestLogger.LogStructuredFailure(logger, new InvalidOperationException("failure"), "retryable");

        // Assert
        var exported = exportedLogs.ShouldHaveSingleItem(item =>
            string.Equals(item.CategoryName, typeof(ServiceDefaultsPrivacyTelemetryTests).FullName, StringComparison.Ordinal));
        exported.Attributes.ShouldContain(static attribute =>
            string.Equals(attribute.Key, "Outcome", StringComparison.Ordinal)
            && string.Equals(attribute.Value as string, "retryable", StringComparison.Ordinal));
    }

    [Fact]
    public void Trace_export_removes_the_business_attributes()
    {
        // Arrange
        var exportedActivities = new ConcurrentQueue<Activity>();
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = string.Empty;
        HostDefaults.Add(builder);
        using var activitySource = new ActivitySource($"privacy-test-{Guid.CreateVersion7()}");
        builder.Services.AddOpenTelemetry().WithTracing(tracing =>
            tracing.AddSource(activitySource.Name)
                .AddProcessor(new SimpleActivityExportProcessor(new CollectingActivityExporter(exportedActivities))));
        using var host = builder.Build();
        var tracerProvider = host.Services.GetRequiredService<TracerProvider>();
        var customerId = Guid.CreateVersion7().ToString();
        var bookingId = Guid.CreateVersion7().ToString();
        string[] sensitive =
        [
            "payment.account.iban", "financial.gross", "recipient.pix", "fx.quote.reference", "receipt.number", "refund.amount",
            "reconciliation.reference", "allocation.amount", "customer.id", "customerId", "customer_id", "booking.id", "bookingId",
            "booking_id", "CustomerEmail", "customer.email", "customer_email", "KeyId",
        ];

        // Act
        using (var activity = activitySource.StartActivity("privacy.failure", ActivityKind.Internal))
        {
            activity.ShouldNotBeNull();
            foreach (var name in sensitive)
            {
                activity.SetTag(name, name.Contains("booking", StringComparison.OrdinalIgnoreCase) ? bookingId : customerId);
            }

            activity.SetTag("outcome", "conflict");
        }

        tracerProvider.ForceFlush();

        // Assert
        var exported = exportedActivities.ShouldHaveSingleItem(item => item.OperationName == "privacy.failure");
        foreach (var name in sensitive)
        {
            exported.GetTagItem(name).ShouldBeNull(name);
        }

        exported.GetTagItem("outcome").ShouldBe("conflict");
    }

    [Fact]
    public void Log_export_removes_customer_identifiers_and_data_protection_key_identifiers()
    {
        // Arrange
        var exportedLogs = new ConcurrentQueue<CapturedLogRecord>();
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = string.Empty;
        HostDefaults.Add(builder);
        builder.Logging.AddOpenTelemetry(logging =>
            logging.AddProcessor(_ => new SimpleLogRecordExportProcessor(new CollectingLogRecordExporter(exportedLogs))));
        using var host = builder.Build();
        var logger = host.Services.GetRequiredService<ILogger<ServiceDefaultsPrivacyTelemetryTests>>();
        var customerId = Guid.CreateVersion7();
        var bookingId = Guid.CreateVersion7();
        var keyId = Guid.CreateVersion7();
        const string email = "traveler@example.com";

        // Act
        PrivacyTestLogger.LogFailure(logger, new InvalidOperationException("failure"), customerId, bookingId, email, "object", "conflict");
        PrivacyTestLogger.LogDataProtectionKey(logger, keyId);

        // Assert
        var exported = exportedLogs.Where(item =>
            string.Equals(item.CategoryName, typeof(ServiceDefaultsPrivacyTelemetryTests).FullName, StringComparison.Ordinal)).ToArray();
        exported.Length.ShouldBe(2);
        var exportedText = string.Join('|', exported.SelectMany(static record => record.Attributes)
            .Select(static attribute => attribute.Value?.ToString() ?? string.Empty));
        exportedText.ShouldNotContain(customerId.ToString(), StringComparison.OrdinalIgnoreCase);
        exportedText.ShouldNotContain(bookingId.ToString(), StringComparison.OrdinalIgnoreCase);
        exportedText.ShouldNotContain(keyId.ToString(), StringComparison.OrdinalIgnoreCase);
        exportedText.ShouldNotContain(email, StringComparison.Ordinal);
        exported.SelectMany(static record => record.Attributes).ShouldContain(static attribute =>
            string.Equals(attribute.Key, "Outcome", StringComparison.Ordinal)
            && string.Equals(attribute.Value as string, "conflict", StringComparison.Ordinal));
    }
}
