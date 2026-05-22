namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

public class BookingTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Can_GetBooking()
    {
        var response = await fixture.Client.GetAsync(fixture.BaseUri, TestContext.Current.CancellationToken);
        Assert.NotNull(response);
    }
}
