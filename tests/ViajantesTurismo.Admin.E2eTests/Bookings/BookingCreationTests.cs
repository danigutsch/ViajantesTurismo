using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.E2ETests.Bookings;

public class BookingCreationTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Can_Create_Booking_From_Customer_Details_With_Prefilled_Data()
    {
        // Arrange
        var tour = await ApiClient.CreateTour(name: "Owned Cultural Experience");
        var customer = await ApiClient.CreateCustomer(
            firstName: "Elena",
            lastName: "Owned",
            bikeType: BikeTypeDto.EBike);
        var customerFullName = $"{customer.FirstName} {customer.LastName}";

        // Act
        await NavigateTo($"/customers/{customer.Id}");
        await Expect(Page).ToHaveTitleAsync("Customer Details");
        await Expect(Page.GetByText(customerFullName).First).ToBeVisibleAsync();

        // Click "Add Booking" to show the inline booking creation form
        await Page.GetButton("Add Booking").ClickAsync();
        await Expect(Page.GetByText("Create New Booking")).ToBeVisibleAsync();

        var bookingForm = Page.Locator("form:has(button:text('Create Booking'))");
        await Expect(bookingForm).ToBeVisibleAsync();

        // Assert: bike type is pre-filled from the owned customer's EBike preference.
        var bikeTypeSelect = bookingForm.Locator("div.mb-3")
            .Filter(new LocatorFilterOptions { HasText = "Bike Type" }).First.Locator("select");
        await Expect(bikeTypeSelect).ToHaveValueAsync("EBike");

        // Assert: no customer select exists because the details page pre-determines the principal customer.
        var customerFields = bookingForm.Locator("div.mb-3")
            .Filter(new LocatorFilterOptions { HasText = "Customer" });
        await Expect(customerFields).ToHaveCountAsync(0);

        // Select the owned test tour (label includes dynamic date, so find the matching option by name).
        var tourSelect = bookingForm.Locator("div.mb-3")
            .Filter(new LocatorFilterOptions { HasText = "Tour" }).First.Locator("select");
        var ownedTourOption = tourSelect.Locator("option", new LocatorLocatorOptions { HasText = tour.Name });
        var optionValue = await ownedTourOption.GetAttributeAsync("value");
        await tourSelect.SelectOptionAsync(optionValue!);

        // Assert: availability and price breakdown appear for the selected owned tour.
        await Expect(bookingForm.GetByText("available")).ToBeVisibleAsync();
        await Expect(bookingForm.GetByText("Price Breakdown")).ToBeVisibleAsync();

        // Act: submit the booking.
        await bookingForm.Locator("#notes").FillAsync("E2E test booking from customer details");
        await bookingForm.GetButton("Create Booking").ClickAsync();

        // Assert: success toast and resulting customer-bookings row use the owned tour.
        await UiFeedback.ExpectToast("Booking created successfully");
        await Expect(Page.GetByText(tour.Name).First).ToBeVisibleAsync();
    }
}
