using Microsoft.Playwright;

namespace ViajantesTurismo.Admin.E2ETests.Tests;

public class ErrorHandlingTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Returns_NotFound_For_Invalid_Routes_And_Guids()
    {
        var randomGuid = Guid.NewGuid().ToString();

        // /tours/{random-guid} → "Tour not found."
        await NavigateToAsync($"/tours/{randomGuid}");
        await Expect(Page.GetByText("Tour not found.")).ToBeVisibleAsync();

        // /customers/{random-guid} → "Customer not found."
        await NavigateToAsync($"/customers/{randomGuid}");
        await Expect(Page.GetByText("Customer not found.")).ToBeVisibleAsync();

        // /edittour/{random-guid} → "Tour not found."
        await NavigateToAsync($"/edittour/{randomGuid}");
        await Expect(Page.GetByText("Tour not found.")).ToBeVisibleAsync();

        // /bookings/{random-guid} → "Booking not found." with no "Edit Booking" link
        await NavigateToAsync($"/bookings/{randomGuid}");
        await Expect(Page.GetByText("Booking not found.")).ToBeVisibleAsync();
        await Expect(Page.GetLink("Back to List")).ToBeVisibleAsync();
        await Expect(Page.GetLink("Edit Booking")).Not.ToBeVisibleAsync();

        // /bookings/{random-guid}/edit → similar error
        await NavigateToAsync($"/bookings/{randomGuid}/edit");
        await Expect(Page.GetByRole(AriaRole.Alert)).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Can_Show_Empty_States_On_All_List_Pages()
    {
        // Clear the database to test empty states (base class seeds by default)
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await Fixture.ClearDatabase(cts.Token);

        // Tour list: no rows, no error
        await NavigateToAsync("/tours");
        await Expect(Page.GetHeading("Tours")).ToBeVisibleAsync();
        await Expect(Page.Locator("table tbody tr")).ToHaveCountAsync(0);

        // Customer list: "No customers found" with create link
        await NavigateToAsync("/customers");
        await Expect(Page.GetByText("No customers found")).ToBeVisibleAsync();
        await Expect(Page.GetLink("Create your first customer")).ToBeVisibleAsync();

        // Booking list: "No bookings found."
        await NavigateToAsync("/bookings");
        await Expect(Page.GetByText("No bookings found")).ToBeVisibleAsync();
    }
}
