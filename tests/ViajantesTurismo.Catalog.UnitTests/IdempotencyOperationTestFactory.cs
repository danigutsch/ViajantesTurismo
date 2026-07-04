using SharedKernel.Idempotency;

namespace ViajantesTurismo.Catalog.UnitTests;

internal static class IdempotencyOperationTestFactory
{
    public static IdempotencyOperation Create() =>
        new(
            IdempotencyScope.From("integration-event:admin.tour.created"),
            IdempotencyKey.From(Guid.CreateVersion7().ToString("N")));
}
