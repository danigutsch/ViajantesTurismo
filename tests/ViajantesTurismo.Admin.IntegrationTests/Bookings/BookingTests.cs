namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

[Trait(TestTraits.CategoryName, TestTraits.SmokeCategory)]
[Trait(TestTraits.ScopeName, TestTraits.IntegrationScope)]
[Trait(TestTraits.AreaName, TestTraits.BookingsArea)]
public class BookingTests(ApiFixture fixture)
{
    [Fact]
    public async Task Can_GetBookings_Smoke()
    {
        var response = await fixture.Client.GetAsync(new Uri("/bookings", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
