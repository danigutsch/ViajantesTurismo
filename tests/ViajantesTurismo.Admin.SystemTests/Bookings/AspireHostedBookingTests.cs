using System.Text.RegularExpressions;

namespace ViajantesTurismo.Admin.SystemTests.Bookings;

public class AspireHostedBookingTests(AspireSystemTestFixture fixture) : AspireSystemTestBase<AspireSystemTestFixture>(fixture)
{
    [Fact]
    public async Task AppHost_managed_fixture_exposes_loopback_endpoints_and_loads_the_web_app()
    {
        // Act
        await NavigateTo("/");

        // Assert
        Assert.True(ApiBaseUri.IsLoopback);
        Assert.True(ApiBaseUri.Port > 0);
        Assert.True(Fixture.WebAppUrl.IsLoopback);
        Assert.True(Fixture.WebAppUrl.Port > 0);
        await Expect(Page).ToHaveURLAsync(new Regex("/$"));
    }
}
