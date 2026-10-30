using PandaBear.Testing;
using ViajantesTurismo.HostedTests.Admin.Api.Infrastructure;
using ViajantesTurismo.Admin.Testing.Integration.Helpers;
namespace ViajantesTurismo.HostedTests.Admin.Api.Bookings;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.SmokeCategory)]
[Trait(PandaBear.Testing.TestTraitNames.AreaName, TestTraits.BookingsArea)]
[TestScope(TestScope.Integration)]
public sealed class BookingBaselineIsolationTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task New_tour_has_no_bookings()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tour = await fixture.Client.CreateTestTour(
            "empty-bookings",
            "Empty bookings",
            cancellationToken);

        // Act
        var bookings = await fixture.Client.GetBookingsByTourAndRead(tour.Id, cancellationToken);

        // Assert
        bookings.ShouldBeEmpty();
    }
}
