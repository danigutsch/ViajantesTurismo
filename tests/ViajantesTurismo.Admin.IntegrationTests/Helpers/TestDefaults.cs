namespace ViajantesTurismo.Admin.IntegrationTests.Helpers;

/// <summary>
/// Test constants and default values for integration tests.
/// </summary>
internal static class TestDefaults
{
    public const decimal BaseTourPrice = 2000m;
    public const decimal DoubleRoomSupplement = 500m;
    public const decimal RegularBikePrice = 100m;
    public const decimal EBikePrice = 200m;
    public const decimal ValidPercentageDiscount = 10m;
    public const decimal ValidAbsoluteDiscount = 150m;
    public const decimal OverAllowedPercentageDiscount = 150m;
    public const decimal AbsoluteDiscountExceedingSubtotal = 3000m;
    public const decimal FirstPaymentAmount = 1000m;
    public const decimal PaymentAmountExceedingRemainingBalance = 3000m;
    public const int MinCustomers = 4;
    public const int MaxCustomers = 12;
}
