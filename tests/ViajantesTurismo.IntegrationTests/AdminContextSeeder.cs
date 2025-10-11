using ViajantesTurismo.ApiService;
using ViajantesTurismo.Common.Monies;

namespace ViajantesTurismo.IntegrationTests;

internal sealed class AdminContextSeeder(ApplicationDbContext dbContext)
{
    private static readonly Tour[] Tours =
    [
        new(
            "CITY001",
            "City Highlights",
            DateTime.Now.AddDays(1),
            DateTime.Now.AddDays(3),
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
            DateTime.Now.AddDays(4),
            DateTime.Now.AddDays(6),
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
            DateTime.Now.AddDays(7),
            DateTime.Now.AddDays(10),
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
            DateTime.Now.AddDays(11),
            DateTime.Now.AddDays(15),
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
            DateTime.Now.AddDays(16),
            DateTime.Now.AddDays(20),
            2500m,
            500m,
            200m,
            300m,
            Currency.Euro,
            ["Hotel", "Breakfast", "Wine Tasting"]
        )
    ];

    public async Task Seed(CancellationToken ct)
    {
        await dbContext.Database.EnsureCreatedAsync(ct);

        dbContext.Tours.AddRange(Tours);

        await dbContext.SaveChangesAsync(ct);
    }
}