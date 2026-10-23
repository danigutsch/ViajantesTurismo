namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Converts controlled UTC instants to the configured payment business calendar; never invents settlement instants.</summary>
public sealed class PaymentBusinessCalendar(TimeProvider clock, TimeZoneInfo timeZone)
{
    /// <summary>Gets the current configured civil business date.</summary>
    public DateOnly Today => At(clock.GetUtcNow());
    /// <summary>Gets the civil business date for an evidenced instant.</summary>
    public DateOnly At(DateTimeOffset instant) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, timeZone).DateTime);
}
