using SharedKernel.Idempotency;
using ViajantesTurismo.Catalog.Application.IntegrationEvents;

namespace ViajantesTurismo.Catalog.Testing.Infrastructure;

/// <summary>Leaves durable observations to the PostgreSQL acceptance tests.</summary>
public sealed class NoOpCatalogInboxDuplicateRecorder : ICatalogInboxDuplicateRecorder
{
    /// <summary>Gets the recorder used by the in-memory Catalog fixtures.</summary>
    public static NoOpCatalogInboxDuplicateRecorder Instance { get; } = new();

    /// <inheritdoc />
    public Task Record(IdempotencyScope scope, Guid eventId, DateTimeOffset observedAt, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
