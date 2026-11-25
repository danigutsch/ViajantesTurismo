using Microsoft.Playwright;
using SharedKernel.Testing;
using SharedKernel.Testing.Playwright;
using ViajantesTurismo.Admin.Testing.Builders;
using ViajantesTurismo.Admin.Testing.Integration.Helpers;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Bases;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Fixtures;

namespace ViajantesTurismo.HostedTests.Admin.System.Shared;

[TestScope(TestScope.System)]
public class CrossEntityNavigationTests(AspireSystemTestFixture fixture) : AspireSystemTestBase<AspireSystemTestFixture>(fixture)
{
    private const string CustomerDetailsTitle = "Customer Details";

    [Fact]
    public async Task Booking_details_should_navigate_to_related_tour_and_customer_details()
    {
        // Arrange
        var tour = await ApiClient.CreateTour();
        var customer = await ApiClient.CreateCustomer();
        var companion = await ApiClient.CreateCustomer(customer.FirstName, customer.LastName);
        var request = DtoBuilders.BuildCreateBookingDto(
            tourId: tour.Id,
            principalCustomerId: customer.Id,
            companionCustomerId: companion.Id);
        var booking = await ApiClient.CreateBookingAndRead(request, TestContext.Current.CancellationToken);
        var bookingPath = $"/bookings/{booking.Id}";
        var tourPath = $"/tours/{tour.Id}";
        var customerPath = $"/customers/{customer.Id}";
        var companionPath = $"/customers/{companion.Id}";
        var customerName = $"{customer.FirstName} {customer.LastName}";
        var customerLinks = Page.GetByRole(AriaRole.Main).GetByRole(AriaRole.Link, new()
        {
            Name = customerName,
            Exact = true
        });

        // Act
        await NavigateTo(bookingPath);
        var tourLink = Page.Locator($"a[href='{tourPath}']");
        await CrossEntityNavigationTestHelpers.FollowLinkAndExpectTitle(Page, tourLink, "Tour Details");
        await Expect(Page).ToHaveURLAsync(new Uri(Fixture.WebAppUrl, tourPath).AbsoluteUri);

        await NavigateTo(bookingPath);
        await Expect(customerLinks).ToHaveCountAsync(2);
        var principalLink = customerLinks.And(Page.Locator($"a[href='{customerPath}']"));
        await CrossEntityNavigationTestHelpers.FollowLinkAndExpectTitle(Page, principalLink, CustomerDetailsTitle);
        await Expect(Page).ToHaveURLAsync(new Uri(Fixture.WebAppUrl, customerPath).AbsoluteUri);

        await NavigateTo(bookingPath);
        var companionLink = customerLinks.And(Page.Locator($"a[href='{companionPath}']"));
        await CrossEntityNavigationTestHelpers.FollowLinkAndExpectTitle(Page, companionLink, CustomerDetailsTitle);

        // Assert
        await Expect(Page).ToHaveURLAsync(new Uri(Fixture.WebAppUrl, companionPath).AbsoluteUri);
    }

    [Fact]
    public async Task Tour_details_should_show_contextual_bookings_without_tour_column()
    {
        // Arrange
        var tour = await ApiClient.CreateTour();
        var customer = await ApiClient.CreateCustomer();
        var booking = await ApiClient.CreateBooking(tour.Id, customer.Id);

        // Act
        await NavigateTo($"/tours/{tour.Id}");

        // Assert
        await Page.ShouldHaveTitle("Tour Details");
        var tourBookingsTable = Page.Locator("table");
        await Expect(tourBookingsTable).ToBeVisibleAsync();
        await CrossEntityNavigationTestHelpers.ExpectColumnVisibility(tourBookingsTable, hiddenHeader: "Tour", visibleHeader: "Customer");
        await CrossEntityNavigationTestHelpers.FollowLinkAndExpectTitle(Page, tourBookingsTable.Locator($"a[href='/bookings/{booking.Id}']"), "Booking Details");
    }

    [Fact]
    public async Task Customer_details_should_show_contextual_bookings_without_customer_column()
    {
        // Arrange
        var tour = await ApiClient.CreateTour();
        var customer = await ApiClient.CreateCustomer();
        var booking = await ApiClient.CreateBooking(tour.Id, customer.Id);

        // Act
        await NavigateTo($"/customers/{customer.Id}");

        // Assert
        await Page.ShouldHaveTitle(CustomerDetailsTitle);
        var customerBookingsTable = Page.Locator("table");
        await Expect(customerBookingsTable).ToBeVisibleAsync();
        await CrossEntityNavigationTestHelpers.ExpectColumnVisibility(customerBookingsTable, hiddenHeader: "Customer", visibleHeader: "Tour");
        await Expect(customerBookingsTable.Locator($"a[href='/bookings/{booking.Id}']")).ToBeVisibleAsync();
    }
}
