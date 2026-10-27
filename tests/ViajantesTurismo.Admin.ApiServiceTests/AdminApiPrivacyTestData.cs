using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.ApiServiceTests;

/// <summary>Builds domain test data for admin API privacy tests.</summary>
internal static class AdminApiPrivacyTestData
{
    /// <summary>Creates a valid tour with the given identifier, starting two months from now and lasting ten days.</summary>
    public static Tour CreateTour(string identifier)
    {
        var startDate = TimeProvider.System.GetUtcNow().UtcDateTime.AddMonths(2);
        var result = Tour.Create(new TourDefinition(
            identifier,
            $"Tour {identifier}",
            DateOnly.FromDateTime(startDate),
            DateOnly.FromDateTime(startDate.AddDays(10)),
            2000m,
            500m,
            100m,
            200m,
            Currency.UsDollar,
            4,
            12,
            ["Hotel", "Breakfast"]));

        return result.IsSuccess
            ? result.Value
            : throw new InvalidOperationException("Failed to create privacy test tour.");
    }
}
