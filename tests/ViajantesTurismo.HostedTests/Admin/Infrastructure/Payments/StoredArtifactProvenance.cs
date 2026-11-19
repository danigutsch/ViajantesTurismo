namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Payments;

/// <summary>Captures persisted metadata for a finalized payment artifact.</summary>
/// <param name="RendererVersion">The recorded renderer build identity.</param>
/// <param name="ContentType">The representation's media type.</param>
/// <param name="RetentionPolicy">The retention policy governing the revision.</param>
/// <param name="RetentionExpiresAt">The disposition date, or null under indefinite retention.</param>
internal sealed record StoredArtifactProvenance(
    string? RendererVersion,
    string? ContentType,
    string? RetentionPolicy,
    DateTime? RetentionExpiresAt);
