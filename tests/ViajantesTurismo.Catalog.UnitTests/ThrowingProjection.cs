using SharedKernel.EventSourcing;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class ThrowingProjection : IProjection
{
    public string Name => "catalog.throwing";

    public ValueTask Apply(EventEnvelope envelope, CancellationToken ct) => throw new InvalidOperationException("projection failed");
}
