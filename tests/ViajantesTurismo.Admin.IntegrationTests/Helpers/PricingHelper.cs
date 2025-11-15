namespace ViajantesTurismo.Admin.IntegrationTests.Helpers;

/// <summary>
/// Helper methods for calculating expected prices in integration tests.
/// </summary>
internal static class PricingHelper
{
    /// <summary>
    /// Calculates the expected booking price based on components and discounts.
    /// </summary>
    public static decimal CalculateExpectedBookingPrice(
        decimal basePrice,
        decimal roomSupplement,
        decimal principalBikePrice,
        decimal? companionBikePrice = null,
        decimal? discountPercentage = null,
        decimal? absoluteDiscount = null)
    {
        var totalPrice = basePrice + roomSupplement + principalBikePrice;

        if (companionBikePrice.HasValue)
        {
            totalPrice += companionBikePrice.Value;
        }

        if (discountPercentage.HasValue)
        {
            totalPrice -= totalPrice * (discountPercentage.Value / 100m);
        }

        if (absoluteDiscount.HasValue)
        {
            totalPrice -= absoluteDiscount.Value;
        }

        return totalPrice;
    }
}
