using System.Text;
using System.Text.Json;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Infrastructure.Branding;
using ViajantesTurismo.Admin.UnitTests.Application.Branding;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.PersistenceCategory)]
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
    public void The_admin_worker_builds_the_admin_model_its_migrations_describe()
    {
        // Arrange
        using var scenario = AdminIntegrationEventWorkerScenario.ComposeWorker();

        // Act
        var hasPendingChanges = scenario.HasPendingModelChanges<AdminWriteDbContext>();

        // Assert
        hasPendingChanges.ShouldBeFalse();
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_transport_envelope_reaches_the_projection_through_the_worker_handler(bool registerDeliveryTwice)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scenario = AdminIntegrationEventWorkerScenario.InMemoryProjection(registerDeliveryTwice);
        var incoming = BrandingProjectionTestData.CreateEvent(revision: 4, brandName: "Delivered");

        // Act
        await scenario.Deliver(incoming, ct);

        // Assert
        var (current, inboxCount) = await scenario.ReadProjection(ct);
        current.ShouldNotBeNull().SourceRevision.ShouldBe(4);
        current.BrandName.ShouldBe("Delivered");
        inboxCount.ShouldBe(1);
    }

    [Fact]
    public async Task An_oversized_transport_payload_cannot_write_the_projection_or_inbox()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scenario = AdminIntegrationEventWorkerScenario.InMemoryProjection();
        var incoming = BrandingProjectionTestData.CreateEvent(revision: 1, brandName: new string('x', 64 * 1024));

        // Act
        var deliver = () => scenario.Deliver(incoming, ct);

        // Assert
        await deliver.ShouldThrow<InvalidOperationException>();
        var (current, inboxCount) = await scenario.ReadProjection(ct);
        current.ShouldBeNull();
        inboxCount.ShouldBe(0);
    }

    [Fact]
    public async Task The_worker_accepts_the_exact_utf8_payload_limit_and_rejects_one_more_byte()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scenario = AdminIntegrationEventWorkerScenario.InMemoryProjection();
        var incoming = BrandingProjectionTestData.CreateEvent(revision: 1);
        var json = JsonSerializer.Serialize(incoming, BrandingIntegrationEventJsonContext.Default.BrandingSettingsChangedIntegrationEvent);
        var prefix = string.Concat(json.AsSpan(0, json.Length - 1), ",\"padding\":\"");
        const string suffix = "\"}";
        var remaining = (64 * 1024) - Encoding.UTF8.GetByteCount(prefix) - suffix.Length;
        var extra = remaining % 2 == 0 ? string.Empty : "x";
        var exact = string.Concat(prefix, new string('é', remaining / 2), extra, suffix);
        var oversized = exact.Insert(exact.Length - suffix.Length, "x");

        // Act
        await scenario.Deliver(incoming, exact, ct);
        var deliver = () => scenario.Deliver(incoming, oversized, ct);

        // Assert
        await deliver.ShouldThrow<InvalidOperationException>();
        var (current, inboxCount) = await scenario.ReadProjection(ct);
        current.ShouldNotBeNull().SourceRevision.ShouldBe(1);
        inboxCount.ShouldBe(1);
    }

    [Fact]
    public async Task A_full_revision_applies_its_new_logo_and_a_later_revision_clears_it_across_gaps()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var scenario = AdminIntegrationEventWorkerScenario.InMemoryProjection();
        var historic = BrandingProjectionTestData.CreateEvent(revision: 1, brandName: "Historic");
        byte[] original = [1, 2, 3];
        byte[] changed = [4, 5, 6];

        // Act
        await scenario.Deliver(historic, ct);
        var (historicState, _) = await scenario.ReadProjection(ct);
        await scenario.Deliver(BrandingProjectionTestData.CreateEventWithLogo(3, original), ct);
        var (originalState, _) = await scenario.ReadProjection(ct);
        await scenario.Deliver(BrandingProjectionTestData.CreateEventWithLogo(5, changed), ct);
        await scenario.Deliver(BrandingProjectionTestData.CreateEventWithLogo(4, original), ct);
        var (changedState, _) = await scenario.ReadProjection(ct);
        var cleared = BrandingProjectionTestData.CreateEvent(revision: 7, brandName: "Cleared") with
        {
            LogoUri = null
        };
        await scenario.Deliver(cleared, ct);
        var (clearedState, inboxCount) = await scenario.ReadProjection(ct);

        // Assert
        historicState.ShouldNotBeNull().Logo.ShouldBeNull();
        historicState.LogoUri.ShouldBe(historic.LogoUri);
        originalState.ShouldNotBeNull().Logo.ShouldNotBeNull().Content.ToArray().ShouldBe(original);
        changedState.ShouldNotBeNull().SourceRevision.ShouldBe(5);
        changedState.Logo.ShouldNotBeNull().Content.ToArray().ShouldBe(changed);
        clearedState.ShouldNotBeNull().SourceRevision.ShouldBe(7);
        clearedState.Logo.ShouldBeNull();
        clearedState.LogoUri.ShouldBeNull();
        inboxCount.ShouldBe(5);
    }
}
