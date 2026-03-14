using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.E2ETests.Bookings;

public class BookingFormInteractionTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Can_Toggle_Companion_Fields_By_Room_Type()
    {
        // Arrange
        var tour = await ApiClient.CreateTour(currency: CurrencyDto.UsDollar);
        var customer = await ApiClient.CreateCustomer();
        var companion = await ApiClient.CreateCustomer();
        var customerLabel = $"{customer.FirstName} {customer.LastName} ({customer.Email})";
        var companionLabel = $"{companion.FirstName} {companion.LastName} ({companion.Email})";

        // Act
        var bookingForm = await OpenBookingForm(tour.Id);
        var roomTypeSelect = GetRoomTypeSelect(bookingForm);
        var customerField = GetFieldGroup(bookingForm, "Customer");
        var companionField = GetFieldGroup(bookingForm, "Companion (Optional)");
        var companionBikeField = GetFieldGroup(bookingForm, "Companion Bike");

        // Assert
        await Expect(roomTypeSelect).ToHaveValueAsync("DoubleOccupancy");
        await Expect(companionField.Locator("select")).ToBeVisibleAsync();

        // Act
        await customerField.Locator("select")
            .SelectOptionAsync(new SelectOptionValue { Label = customerLabel });
        await companionField.Locator("select")
            .SelectOptionAsync(new SelectOptionValue { Label = companionLabel });

        // Assert
        await Expect(companionBikeField.Locator("select")).ToBeVisibleAsync();
        await Expect(companionBikeField.Locator("select")).ToHaveValueAsync("Regular");

        // Act
        await roomTypeSelect.SelectOptionAsync("SingleOccupancy");

        // Assert
        await Expect(companionField.Locator("select")).Not.ToBeVisibleAsync();
        await Expect(companionBikeField.Locator("select")).Not.ToBeVisibleAsync();
        await Expect(bookingForm.Locator(".badge.bg-info")).ToContainTextAsync("Single Occupancy");

        // Act
        await roomTypeSelect.SelectOptionAsync("DoubleOccupancy");

        // Assert
        await Expect(companionField.Locator("select")).ToBeVisibleAsync();
        await Expect(companionField.Locator("select")).ToHaveValueAsync("");
        await Expect(companionBikeField.Locator("select")).Not.ToBeVisibleAsync();
    }

    [Fact]
    public async Task Can_See_Live_Price_Breakdown_During_Booking_Creation()
    {
        // Arrange
        var tour = await ApiClient.CreateTour(currency: CurrencyDto.UsDollar);
        var customer = await ApiClient.CreateCustomer();
        var customerLabel = $"{customer.FirstName} {customer.LastName} ({customer.Email})";

        // Act
        var bookingForm = await OpenBookingForm(tour.Id);
        var customerField = GetFieldGroup(bookingForm, "Customer");
        var bikeTypeSelect = GetFieldGroup(bookingForm, "Principal Customer Bike").Locator("select");
        var roomTypeSelect = GetRoomTypeSelect(bookingForm);

        // Assert
        await Expect(bookingForm.GetByText("Price Breakdown")).ToBeVisibleAsync();
        await Expect(bookingForm.GetByText("$ 1,000.00").First).ToBeVisibleAsync();

        // Act
        await customerField.Locator("select")
            .SelectOptionAsync(new SelectOptionValue { Label = customerLabel });

        // Assert
        await Expect(bookingForm.GetByText("$ 1,050.00").First).ToBeVisibleAsync();

        // Act
        await bikeTypeSelect.SelectOptionAsync("EBike");

        // Assert
        await Expect(bookingForm.GetByText("$ 1,100.00").First).ToBeVisibleAsync();

        // Act
        await roomTypeSelect.SelectOptionAsync("SingleOccupancy");

        // Assert
        await Expect(bookingForm.GetByText("$ 1,300.00").First).ToBeVisibleAsync();
        var finalTotalRow = bookingForm.Locator("dd.fw-bold");
        await Expect(finalTotalRow).ToContainTextAsync("$ 1,300.00");

        // Act
        await bookingForm.Locator("#discountType").SelectOptionAsync("Percentage");
        await bookingForm.Locator("#discountAmount").FillAsync("10");
        await bookingForm.Locator("#discountAmount").BlurAsync();

        // Assert
        await Expect(bookingForm.Locator(".text-danger").Filter(
            new LocatorFilterOptions { HasText = "Discount" })).ToBeVisibleAsync();
        await Expect(bookingForm.GetByText("-$ 130.00").First).ToBeVisibleAsync();
        await Expect(finalTotalRow).ToContainTextAsync("$ 1,170.00");
    }

    private async Task<ILocator> OpenBookingForm(Guid tourId)
    {
        await NavigateTo($"/tours/{tourId}");
        await Expect(Page).ToHaveTitleAsync("Tour Details");

        await Page.GetButton("Add Booking").ClickAsync();

        var bookingForm = Page.Locator("form:has(button:text('Create Booking'))");
        await Expect(bookingForm).ToBeVisibleAsync();
        return bookingForm;
    }

    private static ILocator GetFieldGroup(ILocator bookingForm, string fieldLabel)
    {
        return bookingForm.Locator("div.mb-3")
            .Filter(new LocatorFilterOptions { HasText = fieldLabel }).First;
    }

    private static ILocator GetRoomTypeSelect(ILocator bookingForm)
    {
        return GetFieldGroup(bookingForm, "Room Type").Locator("select");
    }
}
