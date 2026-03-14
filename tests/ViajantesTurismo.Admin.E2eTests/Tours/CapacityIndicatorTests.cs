using System.Globalization;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Pages;

namespace ViajantesTurismo.Admin.E2ETests.Tours;

public class CapacityIndicatorTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Tour_Capacity_Badges_Show_Correct_State_On_List_And_Details()
    {
        // Arrange
        var toursListPage = new ToursListPage(Page, NavigateTo, ApiClient.GetAllTours);
        var api = ApiClient;
        var tour = await api.CreateTour(minCustomers: 1, maxCustomers: 10);
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
        await ExpectCapacitySummary($"{currentCount} / 10 customers");

        // Act
        await UpdateCapacity(1, currentCount);

        // Assert
        await ExpectCapacityStateOnListAndDetails(
            toursListPage,
            tour.Id,
            tourName,
            listBadgeSelector: "span.badge.bg-danger",
            listBadgeText: "Full",
            listCapacityText: $"{currentCount} / {currentCount}",
            detailsBadgeSelector: "span.badge.bg-danger",
            detailsBadgeText: "Fully Booked");

        // Act
        var greenMax = currentCount + 3;
        await UpdateCapacity(currentCount, greenMax);

        // Assert
        await ExpectCapacityStateOnListAndDetails(
            toursListPage,
            tour.Id,
            tourName,
            listBadgeSelector: "span.badge.bg-success",
            listBadgeText: "3 spots",
            listCapacityText: $"{currentCount} / {greenMax}",
            detailsBadgeSelector: "span.badge.bg-success",
            detailsBadgeText: "3 spots available");

        // Act
        await UpdateCapacity(currentCount + 5, 20);

        // Assert
        await ExpectCapacityStateOnListAndDetails(
            toursListPage,
            tour.Id,
            tourName,
            listBadgeSelector: "span.badge.bg-warning",
            listBadgeText: "Below Min",
            listCapacityText: $"{currentCount} / 20",
            detailsBadgeSelector: "span.badge.bg-warning",
            detailsBadgeText: "Below Minimum");
    }

    private async Task UpdateCapacity(int minCustomers, int maxCustomers)
    {
        await Page.GetLink("Edit Tour").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Edit Tour");

        await Page.Locator("#minCustomers").FillAsync(minCustomers.ToString(CultureInfo.InvariantCulture));
        await Page.Locator("#maxCustomers").FillAsync(maxCustomers.ToString(CultureInfo.InvariantCulture));
        await Page.GetButton("Update Tour").ClickAsync();
        await Page.CancelTimedRedirect();
    }

    private async Task ExpectCapacitySummary(string expectedText)
    {
        var capacitySection = Page.Locator("h5:has-text('Capacity') + dl");
        await Expect(capacitySection.GetByText(expectedText)).ToBeVisibleAsync();
    }

    private async Task ExpectCapacityStateOnListAndDetails(
        ToursListPage toursListPage,
        Guid tourId,
        string tourName,
        string listBadgeSelector,
        string listBadgeText,
        string listCapacityText,
        string detailsBadgeSelector,
        string detailsBadgeText)
    {
        var tourRow = await toursListPage.GetTourRow(tourId);
        await Expect(tourRow.Locator(listBadgeSelector)).ToContainTextAsync(listBadgeText);
        await Expect(tourRow.Locator("span.text-nowrap")).ToHaveTextAsync(listCapacityText);

        await tourRow.GetLink("View").ClickAsync();
        await Expect(Page.GetHeading(tourName)).ToBeVisibleAsync();
        await Expect(Page.Locator("h5:has-text('Capacity') + dl").Locator(detailsBadgeSelector))
            .ToContainTextAsync(detailsBadgeText);
    }
}
