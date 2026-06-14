namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

[Trait(TestTraits.CategoryName, TestTraits.SmokeCategory)]
[Trait(TestTraits.ScopeName, TestTraits.IntegrationScope)]
[Trait(TestTraits.AreaName, TestTraits.BookingsArea)]
public sealed class BookingBaselineIsolationTests(AspireSerialIntegrationTestFixture fixture)
    : AspireSerialIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Exercise_An_Empty_Bookings_Baseline_Through_Fixture_Owned_Reset_Control()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var bookings = await Client.GetAllBookingsAndRead(cancellationToken);

        // Assert
        Assert.Empty(bookings);
    }
}
