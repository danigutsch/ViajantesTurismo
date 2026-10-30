using PandaBear.Testing;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Bases;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Fixtures;
using System.Text.RegularExpressions;

namespace ViajantesTurismo.HostedTests.Admin.System.Bookings;

[TestScope(TestScope.System)]
public class AspireHostedBookingTests(AspireSystemTestFixture fixture) : AspireSystemTestBase<AspireSystemTestFixture>(fixture)
{
    [Fact]
    public async Task AppHost_managed_fixture_exposes_loopback_endpoints_and_loads_the_web_app()
    {
        // Act
        await NavigateTo("/");

        // Assert
        ApiBaseUri.IsLoopback.ShouldBeTrue();
        ApiBaseUri.Port.ShouldBeGreaterThan(0);
        Fixture.WebAppUrl.IsLoopback.ShouldBeTrue();
        Fixture.WebAppUrl.Port.ShouldBeGreaterThan(0);
        await Expect(Page).ToHaveURLAsync(new Regex("/$"));
    }
}
