using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

public sealed class SharedHostedInfrastructureTests
{
    [Theory]
    [InlineData(HostedProfile.Admin)]
    [InlineData(HostedProfile.System)]
    public void External_infrastructure_bindings_do_not_declare_containers(HostedProfile profile)
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder([]);
        builder.Configuration["HostedTests:SharedInfrastructure"] = "true";
        builder.Configuration["ConnectionStrings:admin-database"] = "Host=localhost;Database=admin;Username=postgres";
        builder.Configuration["ConnectionStrings:catalog-database"] = "Host=localhost;Database=catalog;Username=postgres";
        builder.Configuration["ConnectionStrings:security-database"] = "Host=localhost;Database=security;Username=postgres";
        builder.Configuration["ConnectionStrings:cache"] = "localhost:6379";
        builder.Configuration["HostedTests:Authority"] = "http://localhost:8080/realms/test";
        builder.Configuration["HostedTests:ClamAv:Host"] = "localhost";
        builder.Configuration["HostedTests:ClamAv:Port"] = "3310";
        builder.Configuration["HostedTests:SeaweedFs:Endpoint"] = "http://localhost:8333";
        builder.Configuration["HostedTests:SeaweedFs:AccessKey"] = "synthetic-access";
        builder.Configuration["HostedTests:SeaweedFs:SecretKey"] = "synthetic-secret";
        builder.Configuration["HostedTests:SeaweedFs:Bucket"] = "synthetic-media";
        builder.Configuration["HostedTests:OtlpEndpoint"] = "http://localhost:4317";

        // Act
        AppHost.AppHostComposition.AddProductResources(builder, profile);
        var containers = builder.Resources.OfType<ContainerResource>().ToArray();
        var projects = builder.Resources.OfType<ProjectResource>().ToArray();

        // Assert
        containers.ShouldBeEmpty();
        projects.ShouldContain(resource => resource.Name == ResourceNames.Api);
        projects.ShouldContain(resource => resource.Name == ResourceNames.MigrationService);
        if (profile is HostedProfile.System)
        {
            projects.ShouldContain(resource => resource.Name == ResourceNames.AdminIntegrationEventWorker);
        }
    }
}
