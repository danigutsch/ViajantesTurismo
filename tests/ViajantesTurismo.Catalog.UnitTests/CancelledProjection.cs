using SharedKernel.EventSourcing;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class CancelledProjection : IProjection
{
    public string Name => "catalog.cancelled";

    public ValueTask Apply(EventEnvelope envelope, CancellationToken ct) => throw new OperationCanceledException(ct);
}
