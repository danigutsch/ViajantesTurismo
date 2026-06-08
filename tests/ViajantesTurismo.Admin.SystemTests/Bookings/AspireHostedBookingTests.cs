using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.SystemTests.Bookings;

public class AspireHostedBookingTests(AspireSystemTestFixture fixture) : AspireSystemTestBase<AspireSystemTestFixture>(fixture)
{
    [Fact]
    public async Task Can_Create_Booking_And_Show_Initial_Details_Via_The_AppHost_Managed_System_Fixture()
    {
        // Arrange
        var tour = await ApiClient.CreateTour(new CreateTourOptions { Currency = CurrencyDto.UsDollar });
        var customer = await ApiClient.CreateCustomer();
        var customerFullName = $"{customer.FirstName} {customer.LastName}";
        var customerSelectionLabel = $"{customerFullName} ({customer.Email})";

        // Act
        var createdBookingId = await BookingWorkflow.CreateFromTourDetails(tour, customerFullName, customerSelectionLabel);
        await BookingWorkflow.NavigateToDetails(createdBookingId);

        // Assert
        Assert.True(ApiBaseUri.IsLoopback);
        Assert.True(ApiBaseUri.Port > 0);
        await Expect(Page.GetByText("Pending").First).ToBeVisibleAsync();
        await Expect(Page.GetByText("Unpaid").First).ToBeVisibleAsync();
        await Expect(Page.GetByText(tour.Name).First).ToBeVisibleAsync();
        await Expect(Page.GetByText(customerFullName).First).ToBeVisibleAsync();
        await Expect(Page.GetByText("$ 1,300.00").First).ToBeVisibleAsync();
    }
}
