using ViajantesTurismo.Admin.SystemTests.Infrastructure.Pages;

namespace ViajantesTurismo.Admin.SystemTests.Tours;

public class CapacityIndicatorTests(AspireSystemTestFixture fixture) : AspireSystemTestBase<AspireSystemTestFixture>(fixture)
{
    [Fact]
    public async Task Tour_capacity_badges_show_correct_state_on_list_and_details()
    {
        // Arrange
        var toursListPage = new ToursListPage(Page, NavigateTo, ApiClient.GetAllTours);
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
        await NavigateTo("/tours");
        await Expect(Page.GetHeading("Tours")).ToBeVisibleAsync();

        // Assert
        var tourRow = await toursListPage.GetTourRow(tour.Id);
        var capacityText = await tourRow.Locator("span.text-nowrap").TextContentAsync();
        Assert.NotNull(capacityText);
        Assert.Matches(@"\d+ / \d+", capacityText);

        await tourRow.GetLink("View").ClickAsync();
        await Expect(Page.GetHeading(tourName)).ToBeVisibleAsync();
        await CapacityIndicatorTestHelpers.ExpectCapacitySummary(Page, $"{currentCount} / 10 customers");

        // Act
        await CapacityIndicatorTestHelpers.UpdateCapacity(Page, 1, currentCount);

        // Assert
        await CapacityIndicatorTestHelpers.ExpectCapacityStateOnListAndDetails(
            Page,
            toursListPage.GetTourRow,
            tour.Id,
            tourName,
            new CapacityStateExpectation(
                "span.badge.bg-danger",
                "Full",
                $"{currentCount} / {currentCount}",
                "span.badge.bg-danger",
                "Fully Booked"));

        // Act
        var greenMax = currentCount + 3;
        await CapacityIndicatorTestHelpers.UpdateCapacity(Page, currentCount, greenMax);

        // Assert
        await CapacityIndicatorTestHelpers.ExpectCapacityStateOnListAndDetails(
            Page,
            toursListPage.GetTourRow,
            tour.Id,
            tourName,
            new CapacityStateExpectation(
                "span.badge.bg-success",
                "3 spots",
                $"{currentCount} / {greenMax}",
                "span.badge.bg-success",
                "3 spots available"));

        // Act
        await CapacityIndicatorTestHelpers.UpdateCapacity(Page, currentCount + 5, 20);

        // Assert
        await CapacityIndicatorTestHelpers.ExpectCapacityStateOnListAndDetails(
            Page,
            toursListPage.GetTourRow,
            tour.Id,
            tourName,
            new CapacityStateExpectation(
                "span.badge.bg-warning",
                "Below Min",
                $"{currentCount} / 20",
                "span.badge.bg-warning",
                "Below Minimum"));
    }

}
