using PandaBear.Testing;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Infrastructure.Branding;
using ViajantesTurismo.Admin.UnitTests.Application.Branding;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.PersistenceCategory)]
[Trait(SharedKernelTestTraitNames.ScopeName, TestTraits.UnitScope)]
public sealed class AdminIntegrationEventWorkerTests
{
    [Fact]
    public void The_admin_worker_runs_only_the_branding_transport_consumer()
    {
        // Arrange
        using var scenario = AdminIntegrationEventWorkerScenario.ComposeWorker();

        // Act
        var hostedServices = scenario.HostedServiceNames()
            .Where(name => name.Contains("IntegrationEvent", StringComparison.Ordinal)
                           || name.Contains("Retention", StringComparison.Ordinal))
            .ToArray();

        // Assert
        hostedServices.ShouldBe(["PostgreSqlIntegrationEventTransportConsumerHostedService`1<AdminBrandingTransportDbContext>"]);
    }

    [Fact]
    public void The_admin_worker_reads_the_catalog_transport_table_and_writes_only_the_projection_schema()
    {
        // Arrange
        using var scenario = AdminIntegrationEventWorkerScenario.ComposeWorker();

        // Act
        var transport = scenario.TableOf<AdminBrandingTransportDbContext>("IntegrationEventTransportMessage");
        var projection = scenario.TableOf<AdminWriteDbContext>("BrandingProjectionRecord");
        var inbox = scenario.TableOf<AdminWriteDbContext>("BrandingInboxRecord");

        // Assert
        transport.ShouldBe(("messaging", "transport_messages"));
        projection.ShouldBe(("branding_projection", "settings"));
        inbox.ShouldBe(("branding_projection", "inbox_messages"));
    }

    [Fact]
    public async Task The_projection_store_applies_a_revision_and_ignores_its_redelivery()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scenario = AdminIntegrationEventWorkerScenario.InMemoryProjection();
        var first = BrandingProjectionTestData.CreateEvent(revision: 1, brandName: "First");
        var second = BrandingProjectionTestData.CreateEvent(revision: 2, brandName: "Second");

        // Act
        await scenario.Apply(first, ct);
        await scenario.Apply(second, ct);
        await scenario.Apply(second, ct);

        // Assert
        var (current, inboxCount) = await scenario.ReadProjection(ct);
        current.ShouldNotBeNull().SourceRevision.ShouldBe(2);
        current.BrandName.ShouldBe("Second");
        current.LogoUri.ShouldBe(new Uri("/logo.svg", UriKind.Relative));
        inboxCount.ShouldBe(2);
    }

    [Fact]
    public async Task A_transport_envelope_reaches_the_projection_through_the_worker_handler()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scenario = AdminIntegrationEventWorkerScenario.InMemoryProjection();
        var incoming = BrandingProjectionTestData.CreateEvent(revision: 4, brandName: "Delivered");

        // Act
        await scenario.Deliver(incoming, ct);

        // Assert
        var (current, inboxCount) = await scenario.ReadProjection(ct);
        current.ShouldNotBeNull().SourceRevision.ShouldBe(4);
        current.BrandName.ShouldBe("Delivered");
        inboxCount.ShouldBe(1);
    }
}
