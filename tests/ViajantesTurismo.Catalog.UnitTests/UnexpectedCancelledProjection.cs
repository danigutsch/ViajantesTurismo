using SharedKernel.EventSourcing;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class UnexpectedCancelledProjection : IProjection
{
    public string Name => "catalog.unexpected_cancelled";

    public ValueTask Apply(EventEnvelope envelope, CancellationToken ct) => throw new OperationCanceledException(ct);
}
