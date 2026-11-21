namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Branding;

/// <summary>Lets an owned worker cross the persisted retry boundary without waiting for the configured backoff.</summary>
internal sealed class BrandingWorkerReproductionClock(DateTimeOffset initialTime) : TimeProvider
{
    private long utcTicks = initialTime.UtcTicks;

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => new(Interlocked.Read(ref utcTicks), TimeSpan.Zero);

    /// <summary>Sets the clock before the next poll reads eligible deliveries.</summary>
    public void AdvanceTo(DateTimeOffset time) => Interlocked.Exchange(ref utcTicks, time.UtcTicks);
}
