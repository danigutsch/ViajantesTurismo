using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Idempotency;
using ViajantesTurismo.Admin.Application.Documents;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Writes document processing claims with a fresh context and its own commit.</summary>
internal sealed class DocumentIdempotencyClaimStore(IServiceScopeFactory scopeFactory) : IDocumentIdempotencyClaimStore
{
    /// <inheritdoc />
    public async ValueTask<IdempotencyStartResult> TryStart(
        IdempotencyOperation operation, DateTimeOffset startedAt, TimeSpan lockDuration, string retentionOwner, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IIdempotencyStore>();
        return await store.TryStart(operation, startedAt, lockDuration, retentionOwner, ct);
    }
}
