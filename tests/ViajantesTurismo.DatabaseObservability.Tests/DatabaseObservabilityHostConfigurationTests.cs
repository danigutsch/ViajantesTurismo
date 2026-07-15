using Microsoft.Extensions.Hosting;
using SharedKernel.Observability.Npgsql;
using SharedKernel.Testing;

namespace ViajantesTurismo.DatabaseObservability.Tests;

[Trait(TestTraitNames.ScopeName, "component")]
public sealed class DatabaseObservabilityHostConfigurationTests
{
    [Fact]
    public void Configure_does_not_register_monitoring_when_disabled()
    {
        // Arrange
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Configuration["DatabaseObservability:PostgreSqlIndexHealth:Enabled"] = "false";

        // Act
        DatabaseObservabilityHostConfiguration.Configure(builder);
        var monitoringRegistrations = builder.Services
            .Where(descriptor => descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType?.Assembly == typeof(PostgreSqlIndexHealthTelemetry).Assembly)
            .ToArray();

        // Assert
        monitoringRegistrations.ShouldBeEmpty();
    }

    [Fact]
    public void Configure_does_not_register_monitoring_when_the_enabled_option_is_absent()
    {
        // Arrange
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());

        // Act
        DatabaseObservabilityHostConfiguration.Configure(builder);
        var monitoringRegistrations = builder.Services
            .Where(descriptor => descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType?.Assembly == typeof(PostgreSqlIndexHealthTelemetry).Assembly)
            .ToArray();

        // Assert
        monitoringRegistrations.ShouldBeEmpty();
    }

    [Fact]
    public void Configure_rejects_enabled_monitoring_without_dedicated_connection_strings()
    {
        // Arrange
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Configuration["DatabaseObservability:PostgreSqlIndexHealth:Enabled"] = "true";
        Action configure = () => DatabaseObservabilityHostConfiguration.Configure(builder);

        // Act
        var exception = configure.ShouldThrow<InvalidOperationException>();

        // Assert
        exception.Message.ShouldContain("dedicated Admin and Catalog connection strings", StringComparison.Ordinal);
    }

    [Fact]
    public void Configure_rejects_enabled_monitoring_without_a_catalog_connection_string()
    {
        // Arrange
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Configuration["DatabaseObservability:PostgreSqlIndexHealth:Enabled"] = "true";
        builder.Configuration["ConnectionStrings:admin-index-health"] = "Host=localhost;Database=admin;Username=monitor;Password=test-only";
        Action configure = () => DatabaseObservabilityHostConfiguration.Configure(builder);

        // Act
        var exception = configure.ShouldThrow<InvalidOperationException>();

        // Assert
        exception.Message.ShouldContain("dedicated Admin and Catalog connection strings", StringComparison.Ordinal);
    }

    [Fact]
    public void Configure_registers_one_monitor_for_dedicated_connection_strings()
    {
        // Arrange
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Configuration["DatabaseObservability:PostgreSqlIndexHealth:Enabled"] = "true";
        builder.Configuration["ConnectionStrings:admin-index-health"] = "Host=localhost;Database=admin;Username=monitor;Password=test-only";
        builder.Configuration["ConnectionStrings:catalog-index-health"] = "Host=localhost;Database=catalog;Username=monitor;Password=test-only";

        // Act
        DatabaseObservabilityHostConfiguration.Configure(builder);
        var monitoringRegistrations = builder.Services
            .Where(descriptor => descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType?.Assembly == typeof(PostgreSqlIndexHealthTelemetry).Assembly)
            .ToArray();

        // Assert
        monitoringRegistrations.ShouldHaveSingleItem();
    }

    [Fact]
    public void Configure_preserves_bounded_monitoring_options()
    {
        // Arrange
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Configuration["DatabaseObservability:PostgreSqlIndexHealth:Enabled"] = "true";
        builder.Configuration["DatabaseObservability:PostgreSqlIndexHealth:PollingInterval"] = "00:00:59";
        builder.Configuration["ConnectionStrings:admin-index-health"] = "Host=localhost;Database=admin;Username=monitor;Password=test-only";
        builder.Configuration["ConnectionStrings:catalog-index-health"] = "Host=localhost;Database=catalog;Username=monitor;Password=test-only";
        Action configure = () => DatabaseObservabilityHostConfiguration.Configure(builder);

        // Act
        var exception = configure.ShouldThrow<InvalidOperationException>();

        // Assert
        exception.Message.ShouldContain("invalid polling interval", StringComparison.Ordinal);
    }

    [Fact]
    public void Configure_rejects_a_zero_command_timeout()
    {
        // Arrange
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Configuration["DatabaseObservability:PostgreSqlIndexHealth:Enabled"] = "true";
        builder.Configuration["DatabaseObservability:PostgreSqlIndexHealth:CommandTimeout"] = "00:00:00";
        builder.Configuration["ConnectionStrings:admin-index-health"] = "Host=localhost;Database=admin;Username=monitor;Password=test-only";
        builder.Configuration["ConnectionStrings:catalog-index-health"] = "Host=localhost;Database=catalog;Username=monitor;Password=test-only";
        Action configure = () => DatabaseObservabilityHostConfiguration.Configure(builder);

        // Act
        var exception = configure.ShouldThrow<InvalidOperationException>();

        // Assert
        exception.Message.ShouldContain("command timeout", StringComparison.Ordinal);
    }
}
