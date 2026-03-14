using ViajantesTurismo.Admin.E2ETests.Infrastructure.Bases;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Fixtures;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Helpers;

namespace ViajantesTurismo.Admin.E2ETests.Shared;

public class ErrorHandlingTests(E2EFixture fixture) : E2ESerialTestBase(fixture)
{
    [Fact]
    public async Task Can_Show_Empty_States_On_All_List_Pages()
    {
        // Clear the database to test empty states (base class seeds by default)
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await ClearDatabase(cts.Token);

        // Tour list: no rows, no error
        await NavigateToAsync("/tours");
        await Expect(Page.GetHeading("Tours")).ToBeVisibleAsync();
        await Expect(Page.Locator("table tbody tr")).ToHaveCountAsync(0);

        // Customer list: "No customers found" with a create link
        await NavigateToAsync("/customers");
        await Expect(Page.GetByText("No customers found")).ToBeVisibleAsync();
        await Expect(Page.GetLink("Create your first customer")).ToBeVisibleAsync();

        // Booking list: "No bookings found."
        await NavigateToAsync("/bookings");
        await Expect(Page.GetByText("No bookings found")).ToBeVisibleAsync();
    }
}
