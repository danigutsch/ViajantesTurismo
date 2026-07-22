using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.EntityFrameworkCore;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Infrastructure;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

internal sealed class AdminSeederScenario : IAsyncDisposable
{
    private readonly ServiceProvider provider;
    private readonly Seeder seeder;

    private AdminSeederScenario(ServiceProvider provider)
    {
        this.provider = provider;
        seeder = provider.GetRequiredService<Seeder>();
    }

    private AdminWriteDbContext DbContext => provider.GetRequiredService<AdminWriteDbContext>();

    public static AdminSeederScenario Create()
    {
        var services = new ServiceCollection();
        services.AddIntegrationEventOutbox<AdminWriteDbContext>();
        services.AddDbContext<AdminWriteDbContext>(options =>
        {
            options.UseInMemoryDatabase(Guid.CreateVersion7().ToString("N"));
            services.ApplyDbContextOptionConfigurations<AdminWriteDbContext>(options);
        });
        services.AddScoped(sp => new Seeder(sp.GetRequiredService<AdminWriteDbContext>()));

        return new AdminSeederScenario(services.BuildServiceProvider());
    }

    public Task Seed(CancellationToken ct) => seeder.Seed(ct);

    public async Task AddExistingTour(CancellationToken ct)
    {
        var tour = Tour.Create(new TourDefinition(
            "EXIST001",
            "Existing Tour",
            new TourScheduleDefinition(DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(7)),
            new TourPricingDefinition(1000m, 100m, 100m, 200m, Currency.Real),
            new TourCapacityDefinition(1, 10),
            ["Hotel"])).Value;
        DbContext.Tours.Add(tour);
        await DbContext.SaveChangesAsync(ct);
    }

    public async Task ShouldContainSeedData(CancellationToken ct)
    {
        var tourCount = await DbContext.Tours.CountAsync(ct);
        var customerCount = await DbContext.Customers.CountAsync(ct);
        var bookingCount = await DbContext.Tours.SelectMany(tour => tour.Bookings).CountAsync(ct);

        tourCount.ShouldBe(5);
        customerCount.ShouldBe(15);
        bookingCount.ShouldBe(10);
    }

    public async Task ShouldContainOnlyTours(int expectedTourCount, CancellationToken ct)
    {
        var tourCount = await DbContext.Tours.CountAsync(ct);
        var customerCount = await DbContext.Customers.CountAsync(ct);
        var bookingCount = await DbContext.Tours.SelectMany(tour => tour.Bookings).CountAsync(ct);

        tourCount.ShouldBe(expectedTourCount);
        customerCount.ShouldBe(0);
        bookingCount.ShouldBe(0);
    }

    public async Task KeepOnlyBaselineTours(CancellationToken ct)
    {
        var bookings = await DbContext.Set<Booking>().ToArrayAsync(ct);
        var customers = await DbContext.Customers.ToArrayAsync(ct);
        DbContext.RemoveRange(bookings);
        DbContext.RemoveRange(customers);
        await DbContext.SaveChangesAsync(ct);
        DbContext.ChangeTracker.Clear();
    }

    public async Task RemoveBaselineBookings(CancellationToken ct)
    {
        var bookings = await DbContext.Set<Booking>().ToArrayAsync(ct);
        DbContext.RemoveRange(bookings);
        await DbContext.SaveChangesAsync(ct);
        DbContext.ChangeTracker.Clear();
    }

    public async Task<Guid[]> GetTourIds(CancellationToken ct) =>
        await DbContext.Tours
            .OrderBy(static tour => tour.Id)
            .Select(static tour => tour.Id)
            .ToArrayAsync(ct);

    public async Task<Guid[]> GetCustomerIds(CancellationToken ct) =>
        await DbContext.Customers
            .OrderBy(static customer => customer.Id)
            .Select(static customer => customer.Id)
            .ToArrayAsync(ct);

    public async ValueTask DisposeAsync()
    {
        await provider.DisposeAsync();
    }
}
