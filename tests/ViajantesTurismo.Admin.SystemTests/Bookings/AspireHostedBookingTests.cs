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
        TestAssert.True(ApiBaseUri.IsLoopback);
        TestAssert.True(ApiBaseUri.Port > 0);
        TestAssert.True(Fixture.WebAppUrl.IsLoopback);
        TestAssert.True(Fixture.WebAppUrl.Port > 0);
        await Expect(Page).ToHaveURLAsync(new Regex("/$"));
    }
}
