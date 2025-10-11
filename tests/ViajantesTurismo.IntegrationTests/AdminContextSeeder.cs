using ViajantesTurismo.Admin.Domain;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Common.Monies;

namespace ViajantesTurismo.IntegrationTests;

internal sealed class AdminContextSeeder(ApplicationDbContext dbContext)
{
    private static readonly Tour[] Tours =
    [
        new(
            "CITY001",
            "City Highlights",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(3),
            1500m,
            300m,
            100m,
            200m,
            Currency.Real,
            ["Hotel", "Breakfast", "City Tour"]
        ),
        new(
            "HIST002",
            "Historical Landmarks",
            DateTime.UtcNow.AddDays(4),
            DateTime.UtcNow.AddDays(6),
            2000m,
            400m,
            150m,
            250m,
            Currency.Euro,
            ["Hotel", "Breakfast", "Museum Tickets"]
        ),
        new(
            "CULT001",
            "Cultural Experience",
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(10),
            1800m,
            350m,
            120m,
            220m,
            Currency.UsDollar,
            ["Hotel", "Breakfast", "Cultural Show"]
        ),
        new(
            "NATR001",
            "Nature and Adventure",
            DateTime.UtcNow.AddDays(11),
            DateTime.UtcNow.AddDays(15),
            2200m,
            450m,
            180m,
            280m,
            Currency.Real,
            ["Hotel", "Breakfast", "Hiking Tour"]
        ),
        new(
            "FOWI003",
            "Food and Wine Tour",
            DateTime.UtcNow.AddDays(16),
            DateTime.UtcNow.AddDays(20),
            2500m,
            500m,
            200m,
            300m,
            Currency.Euro,
            ["Hotel", "Breakfast", "Wine Tasting"]
        )
    ];

    public void Seed()
    {
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();

        dbContext.Tours.AddRange(Tours);

        dbContext.SaveChanges();
    }
}
