namespace ViajantesTurismo.Admin.SystemTests.Tours;

public class CapacityIndicatorTests(AspireSystemTestFixture fixture) : AspireSystemTestBase<AspireSystemTestFixture>(fixture)
{
    [Fact]
    public async Task Tour_capacity_badges_show_correct_state_on_details()
    {
        // Arrange
        var api = ApiClient;
        var tour = await api.CreateTour(new CreateTourOptions { MinCustomers = 1, MaxCustomers = 10 });
        var tourName = tour.Name;

        for (var i = 0; i < 3; i++)
        {
            var customer = await api.CreateCustomer();
            var booking = await api.CreateBooking(tour.Id, customer.Id);
            await api.ConfirmBooking(booking.Id);
        }

        const int currentCount = 3;

        // Act
        await NavigateTo($"/tours/{tour.Id}");

        // Assert
        await Expect(Page.GetHeading(tourName)).ToBeVisibleAsync();
        await CapacityIndicatorTestHelpers.ExpectCapacitySummary(Page, $"{currentCount} / 10 customers");

        // Act
        await CapacityIndicatorTestHelpers.UpdateCapacity(Page, 1, currentCount);

        // Assert
        await CapacityIndicatorTestHelpers.ExpectCapacityStateOnDetails(
            Page,
            () => NavigateTo($"/tours/{tour.Id}"),
            tourName,
            new CapacityStateExpectation(
                "Fully Booked",
                $"{currentCount} / {currentCount}"));

        // Act
        var greenMax = currentCount + 3;
        await CapacityIndicatorTestHelpers.UpdateCapacity(Page, currentCount, greenMax);

        // Assert
        await CapacityIndicatorTestHelpers.ExpectCapacityStateOnDetails(
            Page,
            () => NavigateTo($"/tours/{tour.Id}"),
            tourName,
            new CapacityStateExpectation(
                "3 spots available",
                $"{currentCount} / {greenMax}"));

        // Act
        await CapacityIndicatorTestHelpers.UpdateCapacity(Page, currentCount + 5, 20);

        // Assert
        await CapacityIndicatorTestHelpers.ExpectCapacityStateOnDetails(
            Page,
            () => NavigateTo($"/tours/{tour.Id}"),
            tourName,
            new CapacityStateExpectation(
                "Below Minimum",
                $"{currentCount} / 20"));
    }

}
