using System.Text.RegularExpressions;

namespace ViajantesTurismo.Admin.SystemTests.Bookings;

public class AspireHostedBookingTests(AspireSystemTestFixture fixture) : AspireSystemTestBase<AspireSystemTestFixture>(fixture)
{
    [Fact]
    public async Task AppHost_Managed_Fixture_Exposes_Loopback_Endpoints_And_Loads_The_Web_App()
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
