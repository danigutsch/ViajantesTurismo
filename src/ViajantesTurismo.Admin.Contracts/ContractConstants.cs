namespace ViajantesTurismo.Admin.Contracts;

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
    /// The maximum price value for any tour-related pricing.
    /// </summary>
    public const int MaxPrice = 100_000;

    /// <summary>
    /// The maximum length for booking notes.
    /// </summary>
    public const int MaxBookingNotesLength = 2000;

    /// <summary>
    /// The maximum length for medical information fields (allergies and additional info).
    /// </summary>
    public const int MaxMedicalInfoLength = 500;

    /// <summary>
    /// The minimum duration in days for a tour.
    /// </summary>
    public const int MinimumTourDurationDays = 5;

    /// <summary>
    /// The minimum weight value in kilograms for physical information (inclusive).
    /// </summary>
    public const int MinWeightKg = 1;

    /// <summary>
    /// The maximum weight value in kilograms for physical information (inclusive).
    /// </summary>
    public const int MaxWeightKg = 500;

    /// <summary>
    /// The minimum height value in centimeters for physical information (inclusive).
    /// </summary>
    public const int MinHeightCm = 50;

    /// <summary>
    /// The maximum height value in centimeters for physical information (inclusive).
    /// </summary>
    public const int MaxHeightCm = 300;

    /// <summary>
    /// The maximum percentage value for discount (100%).
    /// </summary>
    public const int MaxDiscountPercentage = 100;

    /// <summary>
    /// The maximum length for discount reason.
    /// </summary>
    public const int MaxDiscountReasonLength = 500;

    /// <summary>
    /// The minimum length for discount reason when discount is applied.
    /// </summary>
    public const int MinDiscountReasonLength = 10;

    /// <summary>
    /// The maximum length for payment reference numbers.
    /// </summary>
    public const int MaxReferenceNumberLength = 128;

    /// <summary>
    /// The maximum length for payment notes.
    /// </summary>
    public const int MaxPaymentNotesLength = 500;

    /// <summary>
    /// The minimum number of customers allowed for a tour.
    /// </summary>
    public const int MinTourCustomers = 1;

    /// <summary>
    /// The maximum number of customers allowed for a tour.
    /// </summary>
    public const int MaxTourCustomers = 20;
}
