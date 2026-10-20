namespace ViajantesTurismo.Catalog.UnitTests;

internal sealed class ControlledTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public DateTimeOffset CurrentInstant { get; set; } = utcNow;
    public override DateTimeOffset GetUtcNow() => CurrentInstant;
}
