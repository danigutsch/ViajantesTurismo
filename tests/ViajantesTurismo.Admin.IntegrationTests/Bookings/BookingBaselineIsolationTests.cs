namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.SmokeCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.IntegrationScope)]
[Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.BookingsArea)]
public sealed class BookingBaselineIsolationTests(ApiFixture fixture)
    : AspireSerialIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_exercise_an_empty_bookings_baseline_through_fixture_owned_reset_control()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var bookings = await Client.GetAllBookingsAndRead(cancellationToken);

        // Assert
        (bookings).ShouldBeEmpty();
    }
}
