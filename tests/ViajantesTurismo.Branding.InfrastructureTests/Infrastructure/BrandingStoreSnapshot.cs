using PandaBear.Messaging;
using ViajantesTurismo.Branding.Infrastructure;

namespace ViajantesTurismo.Branding.InfrastructureTests.Infrastructure;

/// <summary>The persisted Branding settings row, its revisions, and its outbox envelopes.</summary>
internal sealed record BrandingStoreSnapshot(
    BrandingSettingsRecord? Current,
    IReadOnlyList<BrandingSettingsRevisionRecord> Revisions,
    IReadOnlyList<EventEnvelope> Outbox);
