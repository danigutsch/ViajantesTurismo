using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.SystemTests.Bookings;

public class BookingCreationTests(AspireSystemTestFixture fixture) : AspireSystemTestBase<AspireSystemTestFixture>(fixture)
{
    [Fact]
    public async Task Can_create_booking_from_customer_details_with_prefilled_data()
    {
        // Arrange
        var tour = await ApiClient.CreateTour(new CreateTourOptions { Name = $"Owned Cultural {Guid.NewGuid():N}"[..30] });
        var customerFirstName = $"Elena{Guid.NewGuid():N}"[..13];
        var customer = await ApiClient.CreateCustomer(
            firstName: customerFirstName,
            lastName: "Owned",
            bikeType: BikeTypeDto.EBike);
        var customerFullName = $"{customer.FirstName} {customer.LastName}";

        // Act
        await NavigateTo($"/customers/{customer.Id}");
        await Expect(Page).ToHaveTitleAsync("Customer Details");
        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Customer Details" })).ToBeVisibleAsync();
        var customerName = Page.GetByText(customerFullName, new PageGetByTextOptions { Exact = true });
        await Expect(customerName).ToHaveCountAsync(1);
        await Expect(customerName).ToBeVisibleAsync();

        // Click "Add Booking" to show the inline booking creation form
        await Page.GetButton("Add Booking").ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Create New Booking" }))
            .ToBeVisibleAsync();

        var bookingForm = Page.Locator("form:has(button:text('Create Booking'))");
        await Expect(bookingForm).ToBeVisibleAsync();

        // Assert: bike type is pre-filled from the owned customer's EBike preference.
        var bikeTypeSelect = bookingForm.Locator("div.mb-3")
            .Filter(new LocatorFilterOptions { HasText = "Bike Type" }).First.Locator("select");
        await Expect(bikeTypeSelect).ToHaveValueAsync("EBike");

        // Assert: principal customer selector is not rendered on customer details page.
        var principalCustomerField = bookingForm.Locator("label", new LocatorLocatorOptions
        {
            HasText = "Principal Customer"
        });
        await Expect(principalCustomerField).ToHaveCountAsync(0);

        // Select the owned test tour (label includes dynamic date, so find the matching option by name).
        var tourSelect = bookingForm.Locator("div.mb-3")
            .Filter(new LocatorFilterOptions { HasText = "Tour" }).First.Locator("select");
        var ownedTourOption = tourSelect.Locator("option", new LocatorLocatorOptions { HasText = tour.Name });
        var optionValue = await ownedTourOption.GetAttributeAsync("value");
        optionValue.ShouldNotBeNull();
        await tourSelect.SelectOptionAsync(optionValue);

        // Assert: availability and price breakdown appear for the selected owned tour.
        await Expect(bookingForm.GetByText("available")).ToBeVisibleAsync();
        await Expect(bookingForm.GetByText("Price Breakdown")).ToBeVisibleAsync();

        // Act: submit the booking.
        await bookingForm.Locator("#notes").FillAsync("E2E test booking from customer details");
        var toastTask = UiFeedback.ExpectToast("Booking created successfully");
        await bookingForm.GetButton("Create Booking").ClickAsync();

        // Assert: success toast and resulting customer-bookings row use the owned tour.
        await toastTask;
        var tourLink = Page.Locator($"a[href='/tours/{tour.Id}']");
        await Expect(tourLink).ToHaveCountAsync(1);
        await Expect(tourLink).ToBeVisibleAsync();
    }
}
