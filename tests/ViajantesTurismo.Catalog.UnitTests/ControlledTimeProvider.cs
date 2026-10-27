namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Time provider whose current time is set by the test.</summary>
/// <param name="utcNow">Initial UTC time.</param>
internal sealed class ControlledTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    /// <summary>Gets or sets the instant returned as the current UTC time.</summary>
    public DateTimeOffset CurrentInstant { get; set; } = utcNow;
    public override DateTimeOffset GetUtcNow() => CurrentInstant;
}
