namespace ViajantesTurismo.Admin.Testing.Fakes;

public sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public DateTimeOffset CurrentInstant { get; set; } = utcNow;

    public TimeSpan AutoAdvance { get; set; }

    public int ReadCount { get; private set; }

    public override DateTimeOffset GetUtcNow()
    {
        var current = CurrentInstant;
        CurrentInstant += AutoAdvance;
        ReadCount++;
        return current;
    }
}
