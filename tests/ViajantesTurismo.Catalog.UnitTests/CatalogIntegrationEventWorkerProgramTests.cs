using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using PandaBear.MalwareScanning.ClamAv;
using PandaBear.Testing;
using ViajantesTurismo.Resources;
using ViajantesTurismo.Catalog.Infrastructure.Media;
using ViajantesTurismo.Catalog.Infrastructure.Tours;

namespace ViajantesTurismo.Catalog.UnitTests;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraitValues.DependencyInjectionCategory)]
[Trait(SharedKernelTestTraitNames.CapabilityName, TestTraits.IntegrationEventTransportCapability)]
public sealed class CatalogIntegrationEventWorkerProgramTests
{
    private const string WorkerAssemblyName = "ViajantesTurismo.Catalog.IntegrationEventWorker";

    [Fact]
    public void The_worker_entry_point_hosts_the_projection_and_reconciliation_workers_with_health_checks()
    {
        // Arrange
        var workerAssembly = Assembly.Load(new AssemblyName(WorkerAssemblyName));
        string[] args =
        [
            "--environment=Production",
            $"--ConnectionStrings:{ResourceNames.CatalogDatabase}=Host=localhost;Database=viajantes-catalog;Username=test;Password=test",
            $"--ConnectionStrings:{ResourceNames.AdminDatabase}=Host=localhost;Database=viajantes-admin;Username=test;Password=test",
            $"--{ClamAvMalwareScannerConfigurationKeys.HostConfigurationKey}=clamav",
            $"--{ClamAvMalwareScannerConfigurationKeys.PortConfigurationKey}=3310",
            $"--{MediaObjectStorageConfiguration.SectionName}:Endpoint=https://seaweedfs.example",
            $"--{MediaObjectStorageConfiguration.SectionName}:Bucket=media",
            $"--{MediaObjectStorageConfiguration.SectionName}:AccessKey=access",
            $"--{MediaObjectStorageConfiguration.SectionName}:SecretKey=secret"
        ];

        // Act
        using var host = EntryPointHost.Build(workerAssembly, args);

        // Assert
        var hostedServiceTypes = host.Services.GetServices<IHostedService>().Select(service => service.GetType()).ToArray();
        hostedServiceTypes.ShouldContain(typeof(CatalogProjectionHostedService));
        hostedServiceTypes.ShouldContain(typeof(MediaObjectReconciliationHostedService));
        host.Services.GetRequiredService<IServiceProviderIsService>().IsService(typeof(HealthCheckService)).ShouldBeTrue();
    }
}
