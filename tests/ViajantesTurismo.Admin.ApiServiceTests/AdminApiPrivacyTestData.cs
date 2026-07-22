using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.ApiServiceTests;

internal static class AdminApiPrivacyTestData
{
    public static Tour CreateTour(string identifier)
    {
        var startDate = DateTime.UtcNow.AddMonths(2);
        var result = Tour.Create(new TourDefinition(
            identifier,
            $"Tour {identifier}",
            startDate,
            startDate.AddDays(10),
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
