using PandaBear.Messaging.IntegrationEvents;
using ViajantesTurismo.Admin.Application.Branding;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents.Branding;

namespace ViajantesTurismo.Admin.IntegrationEventWorker;

/// <summary>Delivers Branding settings revisions to the Admin Branding projection.</summary>
internal sealed class BrandingSettingsChangedIntegrationHandler(BrandingProjectionApplier applier)
    : IIntegrationEventHandler<BrandingSettingsChangedIntegrationEvent>
{
    /// <inheritdoc />
    public async ValueTask Handle(BrandingSettingsChangedIntegrationEvent integrationEvent, CancellationToken ct) =>
        await applier.Apply(integrationEvent, ct).ConfigureAwait(false);
}
