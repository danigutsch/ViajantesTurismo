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
        ApiBaseUri.IsLoopback.ShouldBeTrue();
        var isValid = ApiBaseUri.Port > 0;
        isValid.ShouldBeTrue();
        Fixture.WebAppUrl.IsLoopback.ShouldBeTrue();
        var isValid1 = Fixture.WebAppUrl.Port > 0;
        isValid1.ShouldBeTrue();
        await Expect(Page).ToHaveURLAsync(new Regex("/$"));
    }
}
