using SharedKernel.Idempotency;

namespace ViajantesTurismo.Catalog.Infrastructure;

internal sealed class IdempotencyEntryEntity
{
    public string Scope { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public IdempotencyEntryState State { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public string? ResultFingerprint { get; set; }
}
