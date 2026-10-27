using PandaBear.Messaging;
using ViajantesTurismo.Branding.Infrastructure;

namespace ViajantesTurismo.Branding.InfrastructureTests.Infrastructure;

/// <summary>The persisted Branding settings row, its revisions, and its outbox envelopes.</summary>
/// <param name="Current">The current settings row, or <see langword="null" /> when none exists.</param>
/// <param name="Revisions">The settings revisions ordered by source revision.</param>
/// <param name="Outbox">The outbox envelopes written for the settings changes.</param>
internal sealed record BrandingStoreSnapshot(
    BrandingSettingsRecord? Current,
    IReadOnlyList<BrandingSettingsRevisionRecord> Revisions,
    IReadOnlyList<EventEnvelope> Outbox);
