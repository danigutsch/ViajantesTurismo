using PandaBear.Testing;
using ViajantesTurismo.Branding.Infrastructure;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraitValues.DependencyInjectionCategory)]
[Trait(SharedKernelTestTraitNames.CapabilityName, TestTraits.IntegrationEventTransportCapability)]
public sealed class CatalogBrandingCoHostedMessagingTests
{
    [Fact]
    public void Catalog_code_enqueues_into_the_catalog_outbox_when_branding_registers_first()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateBrandingCoHostedScenario();

        // Act
        var defaultIsCatalog = scenario.DefaultOutboxBelongsTo<CatalogDbContext>();

        // Assert
        defaultIsCatalog.ShouldBeTrue();
    }

    [Fact]
    public void Co_hosted_contexts_keep_separate_outbox_tables()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateBrandingCoHostedScenario();

        // Act
        var catalogOutbox = scenario.OutboxTable<CatalogDbContext>();
        var brandingOutbox = scenario.OutboxTable<BrandingDbContext>();

        // Assert
        catalogOutbox.ShouldBe(("messaging", "outbox_messages"));
        brandingOutbox.ShouldBe(("branding", "outbox_messages"));
    }

    [Fact]
    public void Co_hosted_contexts_each_run_their_own_outbox_relay()
    {
        // Arrange
        using var scenario = CatalogInfrastructureTestServices.CreateBrandingCoHostedScenario();

        // Act
        var catalogRelay = scenario.ContainsHostedService<CatalogDbContext>("IntegrationEventOutboxRelayHostedService`1");
        var brandingRelay = scenario.ContainsHostedService<BrandingDbContext>("IntegrationEventOutboxRelayHostedService`1");

        // Assert
        catalogRelay.ShouldBeTrue();
        brandingRelay.ShouldBeTrue();
    }
}
