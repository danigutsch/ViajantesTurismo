using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;

namespace ViajantesTurismo.ServiceDefaults.Tests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, SharedKernel.Testing.TestTraitValues.SecurityCategory)]
public sealed class ServiceDefaultsPrivacyTelemetryTests
{
    [Fact]
    public void Service_defaults_remove_preconfigured_non_otlp_logging_providers()
    {
        // Arrange
        var capturedMessages = new ConcurrentQueue<string>();
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Production
        });
        builder.Logging.AddProvider(new CollectingLoggerProvider(capturedMessages));
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = string.Empty;

        // Act
        builder.AddServiceDefaults();
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
        capturedMessages.ShouldBeEmpty();
    }

    [Fact]
    public async Task Aspnet_trace_export_keeps_route_and_operational_fields_without_raw_identifiers_or_query_values()
    {
        // Arrange
        var exportedActivities = new ConcurrentQueue<Activity>();
        var serverActivityExported = new TaskCompletionSource<Activity>(TaskCreationOptions.RunContinuationsAsynchronously);
        var customerId = Guid.CreateVersion7();
        var bookingId = Guid.CreateVersion7();
        const string email = "traveler@example.com";
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production
        });
        builder.WebHost.UseTestServer();
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = string.Empty;
        builder.AddServiceDefaults();
        builder.Services.AddOpenTelemetry().WithTracing(tracing =>
            tracing.AddProcessor(new SimpleActivityExportProcessor(new CollectingActivityExporter(
                exportedActivities,
                activity =>
                {
                    if (activity.Kind == ActivityKind.Server)
                    {
                        serverActivityExported.TrySetResult(activity);
                    }
                }))));
        await using var app = builder.Build();
        app.MapGet("/customers/{customerId}/bookings/{bookingId}", static () => "ok");
        await app.StartAsync(TestContext.Current.CancellationToken);
        using var client = app.GetTestClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("private-agent/123");

        // Act
        using var response = await client.GetAsync(
            new Uri($"/customers/{customerId}/bookings/{bookingId}?email={Uri.EscapeDataString(email)}", UriKind.Relative),
            TestContext.Current.CancellationToken);
        await serverActivityExported.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        var activity = exportedActivities.ShouldHaveSingleItem(item => item.Kind == ActivityKind.Server);
        var route = activity.GetTagItem("http.route");
        var method = activity.GetTagItem("http.request.method");
        var host = activity.GetTagItem("server.address");
        var statusCode = activity.GetTagItem("http.response.status_code");
        route.ShouldBe("/customers/{customerId}/bookings/{bookingId}");
        method.ShouldBe("GET");
        host.ShouldBe("localhost");
        Convert.ToInt32(statusCode, System.Globalization.CultureInfo.InvariantCulture).ShouldBe(200);
        activity.GetTagItem("url.path").ShouldBeNull();
        activity.GetTagItem("url.full").ShouldBeNull();
        activity.GetTagItem("url.query").ShouldBeNull();
        activity.GetTagItem("http.target").ShouldBeNull();
        activity.GetTagItem("user_agent.original").ShouldBeNull();
        activity.GetTagItem("http.user_agent").ShouldBeNull();
        var tagValues = activity.TagObjects.Select(tag => tag.Value?.ToString() ?? string.Empty).ToArray();
        tagValues.ShouldNotContain(value => value.Contains(customerId.ToString(), StringComparison.OrdinalIgnoreCase));
        tagValues.ShouldNotContain(value => value.Contains(bookingId.ToString(), StringComparison.OrdinalIgnoreCase));
        tagValues.ShouldNotContain(value => value.Contains(email, StringComparison.Ordinal));
        tagValues.ShouldNotContain(value => value.Contains("private-agent/123", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Aspnet_trace_export_disables_preconfigured_exception_events()
    {
        // Arrange
        const string sensitiveMessage = "traveler@example.com at /customers/private";
        var exportedActivities = new ConcurrentQueue<Activity>();
        var serverActivityExported = new TaskCompletionSource<Activity>(TaskCreationOptions.RunContinuationsAsynchronously);
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production
        });
        builder.WebHost.UseTestServer();
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = string.Empty;
        builder.Services.PostConfigureAll<AspNetCoreTraceInstrumentationOptions>(options => options.RecordException = true);
        builder.AddServiceDefaults();
        builder.Services.AddOpenTelemetry().WithTracing(tracing =>
            tracing.AddProcessor(new SimpleActivityExportProcessor(new CollectingActivityExporter(
                exportedActivities,
                activity =>
                {
                    if (activity.Kind == ActivityKind.Server)
                    {
                        serverActivityExported.TrySetResult(activity);
                    }
                }))));
        await using var app = builder.Build();
        app.MapGet("/failure", static () => Task.FromException(new InvalidOperationException(sensitiveMessage)));
        await app.StartAsync(TestContext.Current.CancellationToken);
        using var client = app.GetTestClient();

        // Act
        Func<Task> request = async () =>
        {
            using var response = await client.GetAsync(
                new Uri("/failure", UriKind.Relative),
                TestContext.Current.CancellationToken);
        };
        _ = await request.ShouldThrow<InvalidOperationException>();
        await serverActivityExported.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        var activity = exportedActivities.ShouldHaveSingleItem(item => item.Kind == ActivityKind.Server);
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.StatusDescription.ShouldBeNull();
        activity.Events.ShouldNotContain(static activityEvent => activityEvent.Name == "exception");
    }

    [Theory]
    [InlineData("")]
    [InlineData("custom")]
    public void Service_defaults_disable_exception_recording_for_all_aspnet_options(string optionsName)
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder();
        builder.Services.PostConfigureAll<AspNetCoreTraceInstrumentationOptions>(options => options.RecordException = true);

        // Act
        builder.AddServiceDefaults();
        using var host = builder.Build();
        var options = host.Services.GetRequiredService<IOptionsMonitor<AspNetCoreTraceInstrumentationOptions>>()
            .Get(optionsName);

        // Assert
        options.RecordException.ShouldBeFalse();
    }

    [Fact]
    public void Trace_export_removes_sensitive_attributes_and_status_description_but_keeps_bounded_error_type()
    {
        // Arrange
        var exportedActivities = new ConcurrentQueue<Activity>();
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = string.Empty;
        builder.AddServiceDefaults();
        using var activitySource = new ActivitySource($"privacy-test-{Guid.CreateVersion7()}");
        builder.Services.AddOpenTelemetry().WithTracing(tracing =>
            tracing.AddSource(activitySource.Name)
                .AddProcessor(new SimpleActivityExportProcessor(new CollectingActivityExporter(exportedActivities))));
        using var host = builder.Build();
        var tracerProvider = host.Services.GetRequiredService<TracerProvider>();
        var customerId = Guid.CreateVersion7();
        var bookingId = Guid.CreateVersion7();
        const string sensitiveMessage = "Duplicate email traveler@example.com";
        const string email = "traveler@example.com";
        const string objectKey = "customers/private/media/passport.jpg";

        // Act
        using (var activity = activitySource.StartActivity("privacy.failure", ActivityKind.Internal))
        {
            activity.ShouldNotBeNull();
            activity.SetTag("customer.id", customerId.ToString());
            activity.SetTag("customerId", customerId.ToString());
            activity.SetTag("booking.id", bookingId.ToString());
            activity.SetTag("bookingId", bookingId.ToString());
            activity.SetTag("CustomerEmail", email);
            activity.SetTag("url.path", $"/customers/{customerId}/bookings/{bookingId}");
            activity.SetTag("url.full", $"https://example.test/customers/{customerId}?email=traveler@example.com");
            activity.SetTag("url.query", "email=traveler@example.com");
            activity.SetTag("http.request.header.authorization", "Bearer private-token");
            activity.SetTag("http.response.header.set-cookie", "session=private");
            activity.SetTag("http.request.body", email);
            activity.SetTag("http.response.body", email);
            activity.SetTag("db.query.parameter.customer_email", email);
            activity.SetTag("media.object_key", objectKey);
            activity.SetTag("aws.s3.key", objectKey);
            activity.SetTag("exception.message", sensitiveMessage);
            activity.SetTag("exception.stacktrace", $"at CustomerHandler({sensitiveMessage})");
            activity.SetTag("diagnostic.failure", new InvalidOperationException(sensitiveMessage));
            activity.SetTag("error.type", nameof(InvalidOperationException));
            activity.SetStatus(ActivityStatusCode.Error, sensitiveMessage);
        }
        tracerProvider.ForceFlush();

        // Assert
        var exported = exportedActivities.ShouldHaveSingleItem(item => item.OperationName == "privacy.failure");
        exported.Status.ShouldBe(ActivityStatusCode.Error);
        exported.StatusDescription.ShouldBeNull();
        exported.GetTagItem("error.type").ShouldBe(nameof(InvalidOperationException));
        exported.GetTagItem("customer.id").ShouldBeNull();
        exported.GetTagItem("customerId").ShouldBeNull();
        exported.GetTagItem("booking.id").ShouldBeNull();
        exported.GetTagItem("bookingId").ShouldBeNull();
        exported.GetTagItem("CustomerEmail").ShouldBeNull();
        exported.GetTagItem("url.path").ShouldBeNull();
        exported.GetTagItem("url.full").ShouldBeNull();
        exported.GetTagItem("url.query").ShouldBeNull();
        exported.GetTagItem("http.request.header.authorization").ShouldBeNull();
        exported.GetTagItem("http.response.header.set-cookie").ShouldBeNull();
        exported.GetTagItem("http.request.body").ShouldBeNull();
        exported.GetTagItem("http.response.body").ShouldBeNull();
        exported.GetTagItem("db.query.parameter.customer_email").ShouldBeNull();
        exported.GetTagItem("media.object_key").ShouldBeNull();
        exported.GetTagItem("aws.s3.key").ShouldBeNull();
        exported.GetTagItem("exception.message").ShouldBeNull();
        exported.GetTagItem("exception.stacktrace").ShouldBeNull();
        exported.GetTagItem("diagnostic.failure").ShouldBeNull();
    }

    [Fact]
    public void Log_export_removes_exception_and_raw_identifiers_while_preserving_operational_outcome()
    {
        // Arrange
        var exportedLogs = new ConcurrentQueue<CapturedLogRecord>();
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = string.Empty;
        builder.AddServiceDefaults();
        builder.Logging.AddOpenTelemetry(logging =>
            logging.AddProcessor(_ => new SimpleLogRecordExportProcessor(new CollectingLogRecordExporter(exportedLogs))));
        using var host = builder.Build();
        var logger = host.Services.GetRequiredService<ILogger<ServiceDefaultsPrivacyTelemetryTests>>();
        var customerId = Guid.CreateVersion7();
        var bookingId = Guid.CreateVersion7();
        const string email = "traveler@example.com";
        const string objectKey = "customers/private/media/passport.jpg";
        using var scope = logger.BeginScope(new Dictionary<string, object?> { ["CustomerEmail"] = email });

        // Act
        PrivacyTestLogger.LogFailure(
            logger,
            new InvalidOperationException($"Duplicate email {email}"),
            customerId,
            bookingId,
            email,
            objectKey,
            "conflict");

        // Assert
        var exported = exportedLogs.ShouldHaveSingleItem(item => string.Equals(
            item.CategoryName,
            typeof(ServiceDefaultsPrivacyTelemetryTests).FullName,
            StringComparison.Ordinal));
        exported.FormattedMessage.ShouldBeNull();
        exported.Exception.ShouldBeNull();
        exported.Scopes.ShouldBeEmpty();
        exported.Body.ShouldBe("Customer {CustomerId} booking {BookingId} email {CustomerEmail} object {ObjectKey} failed with {Outcome}");
        exported.Attributes.ShouldContain(attribute =>
            string.Equals(attribute.Key, "Outcome", StringComparison.Ordinal)
            && string.Equals(attribute.Value as string, "conflict", StringComparison.Ordinal));
        exported.Attributes.ShouldContain(attribute =>
            string.Equals(attribute.Key, "exception.type", StringComparison.Ordinal)
            && string.Equals(attribute.Value as string, typeof(InvalidOperationException).FullName, StringComparison.Ordinal));
        var exportedText = string.Join(
            '|',
            exported.Attributes.Select(attribute => attribute.Value?.ToString() ?? string.Empty)
                .Append(exported.Body ?? string.Empty)
                .Append(exported.FormattedMessage ?? string.Empty)
                .Concat(exported.Scopes.Select(value => value?.ToString() ?? string.Empty)));
        exportedText.ShouldNotContain(customerId.ToString(), StringComparison.OrdinalIgnoreCase);
        exportedText.ShouldNotContain(bookingId.ToString(), StringComparison.OrdinalIgnoreCase);
        exportedText.ShouldNotContain(email, StringComparison.Ordinal);
        exportedText.ShouldNotContain(objectKey, StringComparison.Ordinal);
    }

    [Fact]
    public void Log_export_removes_exception_objects_from_non_sensitive_structured_attributes()
    {
        // Arrange
        var exportedLogs = new ConcurrentQueue<CapturedLogRecord>();
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = string.Empty;
        builder.AddServiceDefaults();
        builder.Logging.AddOpenTelemetry(logging =>
            logging.AddProcessor(_ => new SimpleLogRecordExportProcessor(new CollectingLogRecordExporter(exportedLogs))));
        using var host = builder.Build();
        var logger = host.Services.GetRequiredService<ILogger<ServiceDefaultsPrivacyTelemetryTests>>();
        var failure = new InvalidOperationException("traveler@example.com at /customers/private");

        // Act
        PrivacyTestLogger.LogStructuredFailure(logger, failure, "retryable");

        // Assert
        var exported = exportedLogs.ShouldHaveSingleItem(item => string.Equals(
            item.CategoryName,
            typeof(ServiceDefaultsPrivacyTelemetryTests).FullName,
            StringComparison.Ordinal));
        exported.Attributes.ShouldNotContain(static attribute => attribute.Value is Exception);
        exported.Attributes.ShouldContain(static attribute =>
            string.Equals(attribute.Key, "exception.type", StringComparison.Ordinal)
            && string.Equals(attribute.Value as string, typeof(InvalidOperationException).FullName, StringComparison.Ordinal));
        exported.Attributes.ShouldContain(static attribute =>
            string.Equals(attribute.Key, "Outcome", StringComparison.Ordinal)
            && string.Equals(attribute.Value as string, "retryable", StringComparison.Ordinal));
    }

    [Fact]
    public void Log_export_removes_entity_framework_command_and_parameter_values()
    {
        // Arrange
        var exportedLogs = new ConcurrentQueue<CapturedLogRecord>();
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = string.Empty;
        builder.AddServiceDefaults();
        builder.Logging.AddOpenTelemetry(logging =>
            logging.AddProcessor(_ => new SimpleLogRecordExportProcessor(new CollectingLogRecordExporter(exportedLogs))));
        using var host = builder.Build();
        var logger = host.Services.GetRequiredService<ILogger<ServiceDefaultsPrivacyTelemetryTests>>();
        const string email = "traveler@example.com";

        // Act
        PrivacyTestLogger.LogEntityFrameworkCommand(
            logger,
            "SELECT * FROM customers WHERE email = @email",
            $"@email='{email}'",
            "executed");

        // Assert
        var exported = exportedLogs.ShouldHaveSingleItem(item => string.Equals(
            item.CategoryName,
            typeof(ServiceDefaultsPrivacyTelemetryTests).FullName,
            StringComparison.Ordinal));
        exported.Attributes.ShouldNotContain(static attribute =>
            string.Equals(attribute.Key, "commandText", StringComparison.OrdinalIgnoreCase)
            || string.Equals(attribute.Key, "parameters", StringComparison.OrdinalIgnoreCase));
        exported.Attributes.ShouldContain(static attribute =>
            string.Equals(attribute.Key, "Outcome", StringComparison.Ordinal)
            && string.Equals(attribute.Value as string, "executed", StringComparison.Ordinal));
        var exportedText = string.Join('|', exported.Attributes.Select(attribute => attribute.Value?.ToString() ?? string.Empty));
        exportedText.ShouldNotContain(email, StringComparison.Ordinal);
    }

    [Fact]
    public void Log_export_replaces_existing_exception_type_case_insensitively()
    {
        // Arrange
        var exportedLogs = new ConcurrentQueue<CapturedLogRecord>();
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = string.Empty;
        builder.AddServiceDefaults();
        builder.Logging.AddOpenTelemetry(logging =>
            logging.AddProcessor(_ => new SimpleLogRecordExportProcessor(new CollectingLogRecordExporter(exportedLogs))));
        using var host = builder.Build();
        var logger = host.Services.GetRequiredService<ILogger<ServiceDefaultsPrivacyTelemetryTests>>();
        const string unsafeType = "Sensitive.Exception.Type";

        // Act
        PrivacyTestLogger.LogMixedCaseExceptionType(
            logger,
            unsafeType,
            new InvalidOperationException("traveler@example.com"));

        // Assert
        var exported = exportedLogs.ShouldHaveSingleItem(item => string.Equals(
            item.CategoryName,
            typeof(ServiceDefaultsPrivacyTelemetryTests).FullName,
            StringComparison.Ordinal));
        var exceptionTypes = exported.Attributes
            .Where(static attribute => string.Equals(attribute.Key, "exception.type", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        exceptionTypes.Length.ShouldBe(1);
        exceptionTypes[0].Key.ShouldBe("exception.type");
        exceptionTypes[0].Value.ShouldBe(typeof(InvalidOperationException).FullName);
        exported.Attributes.ShouldNotContain(attribute => string.Equals(attribute.Value as string, unsafeType, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Log_export_limits_error_type_case_insensitively(bool useExceptionType)
    {
        // Arrange
        var exportedLogs = new ConcurrentQueue<CapturedLogRecord>();
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = string.Empty;
        builder.AddServiceDefaults();
        builder.Logging.AddOpenTelemetry(logging =>
            logging.AddProcessor(_ => new SimpleLogRecordExportProcessor(new CollectingLogRecordExporter(exportedLogs))));
        using var host = builder.Build();
        var logger = host.Services.GetRequiredService<ILogger<ServiceDefaultsPrivacyTelemetryTests>>();
        var oversizedType = new string('x', 257);

        // Act
        if (useExceptionType)
        {
            PrivacyTestLogger.LogMixedCaseExceptionType(logger, oversizedType, exception: null);
        }
        else
        {
            PrivacyTestLogger.LogMixedCaseErrorType(logger, oversizedType);
        }

        // Assert
        var exported = exportedLogs.ShouldHaveSingleItem(item => string.Equals(
            item.CategoryName,
            typeof(ServiceDefaultsPrivacyTelemetryTests).FullName,
            StringComparison.Ordinal));
        var errorType = exported.Attributes.ShouldHaveSingleItem(attribute =>
            string.Equals(attribute.Key, useExceptionType ? "exception.type" : "error.type", StringComparison.OrdinalIgnoreCase));
        var value = errorType.Value.ShouldBeOfType<string>();
        value.Length.ShouldBe(256);
    }
}
