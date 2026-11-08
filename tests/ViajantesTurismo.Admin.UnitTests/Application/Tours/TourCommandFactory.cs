using ViajantesTurismo.Admin.Application.Tours.CreateTour;
using ViajantesTurismo.Admin.Application.Tours.UpdateTour;

namespace ViajantesTurismo.Admin.UnitTests.Application.Tours;

/// <summary>Builds tour commands that differ only in the currency code under test.</summary>
internal static class TourCommandFactory
{
    /// <summary>Creates a valid create-tour command carrying the given currency code.</summary>
    public static CreateTourCommand Create(string currency) => new(
        "andes-2026", "Andes 2026", new DateOnly(2026, 3, 10), new DateOnly(2026, 3, 20),
        1_500m, 250m, 100m, 180m, currency, ["Guide"], 4, 12);

    /// <summary>Creates a valid update-tour command carrying the given currency code.</summary>
    public static UpdateTourCommand Update(Guid id, decimal price, string currency) => new(
        id, "andes-2026", "Andes 2026", new DateOnly(2030, 3, 10), new DateOnly(2030, 3, 20),
        price, 250m, 100m, 180m, currency, ["Guide"], 4, 12);
}
