namespace ViajantesTurismo.AdminApi.Contracts;

/// <summary>
/// Contains constant values used in the contract definitions.
/// </summary>
public static class ContractConstants
{
    /// <summary>
    /// The maximum length for default string fields such as identifiers.
    /// </summary>
    public const int MaxDefaultLength = 64;

    /// <summary>
    /// The maximum length for names such as customer names and tour names.
    /// </summary>
    public const int MaxNameLength = 128;

    /// <summary>
    /// The maximum length for service names in the included services list.
    /// </summary>
    public const int MaxServiceDescriptionLength = 256;

    /// <summary>
    /// The minimum price value (exclusive) for any tour-related pricing.
    /// </summary>
    public const int MinPrice = 0;

    /// <summary>
    /// The maximum price value for any tour-related pricing.
    /// </summary>
    public const int MaxPrice = 100_000;

    /// <summary>
    /// The maximum length for booking notes.
    /// </summary>
    public const int MaxBookingNotesLength = 2000;

    /// <summary>
    /// The minimum duration in days for a tour.
    /// </summary>
    public const int MinimumTourDurationDays = 5;
}
