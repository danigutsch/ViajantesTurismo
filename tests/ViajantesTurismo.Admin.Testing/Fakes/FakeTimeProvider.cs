namespace ViajantesTurismo.Admin.Testing.Fakes;

public sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
