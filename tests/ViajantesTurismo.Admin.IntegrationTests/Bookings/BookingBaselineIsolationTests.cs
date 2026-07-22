namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.SmokeCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.IntegrationScope)]
[Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.BookingsArea)]
public sealed class BookingBaselineIsolationTests(ApiFixture fixture)
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
