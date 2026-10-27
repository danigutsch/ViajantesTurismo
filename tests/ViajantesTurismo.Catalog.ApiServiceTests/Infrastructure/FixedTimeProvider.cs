namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

/// <summary>A time provider that always returns one fixed UTC instant.</summary>
/// <param name="utcNow">The instant returned by <see cref="GetUtcNow" />.</param>
internal sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
