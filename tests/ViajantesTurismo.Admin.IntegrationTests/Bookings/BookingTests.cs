using ViajantesTurismo.Admin.Testing.Integration;

namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

[Trait(TestTraits.CategoryName, TestTraits.SmokeCategory)]
[Trait(TestTraits.ScopeName, TestTraits.IntegrationScope)]
[Trait(TestTraits.AreaName, TestTraits.BookingsArea)]
public class BookingTests(ApiFixture fixture)
{
    [Fact]
    public async Task Can_GetBookings_Smoke()
    {
        Assert.IsAssignableFrom<IAdminTestHost>(fixture);

        if (fixture is not IAdminTestHost host)
        {
            throw new InvalidOperationException("ApiFixture must implement IAdminTestHost.");
        }

        var response = await host.Client.GetAsync(new Uri("/bookings", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
