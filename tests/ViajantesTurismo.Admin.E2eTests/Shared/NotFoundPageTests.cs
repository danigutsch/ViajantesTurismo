using ViajantesTurismo.Admin.E2ETests.Infrastructure.Bases;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Fixtures;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Helpers;

namespace ViajantesTurismo.Admin.E2ETests.Shared;

public class NotFoundPageTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Not_Found_Pages_Hide_Action_Links_And_Show_Consistent_Layout()
    {
        var randomGuid = Guid.NewGuid().ToString();

        // === Tour detail not-found ===
        await NavigateTo($"/tours/{randomGuid}");
        await Expect(Page.GetByText("Tour not found.")).ToBeVisibleAsync();
        await Expect(Page.GetLink("Back to Tours")).ToBeVisibleAsync();
        // No edit link should appear
        await Expect(Page.GetLink("Edit Tour")).Not.ToBeVisibleAsync();

        // === Tour edit not-found ===
        await NavigateTo($"/edittour/{randomGuid}");
        await Expect(Page.GetByText("Tour not found.")).ToBeVisibleAsync();
        await Expect(Page.GetLink("Back to Tours")).ToBeVisibleAsync();

        // === Customer detail not-found ===
        await NavigateTo($"/customers/{randomGuid}");
        await Expect(Page.GetByText("Customer not found.")).ToBeVisibleAsync();
        await Expect(Page.GetLink("Back to Customers")).ToBeVisibleAsync();
        await Expect(Page.GetLink("Edit Customer")).Not.ToBeVisibleAsync();

        // === Customer edit not-found ===
        await NavigateTo($"/customers/{randomGuid}/edit");
        await Expect(Page.GetByText("Customer not found.")).ToBeVisibleAsync();
        await Expect(Page.GetLink("Back to Customers")).ToBeVisibleAsync();

        // === Booking detail not-found ===
        await NavigateTo($"/bookings/{randomGuid}");
        await Expect(Page.GetByText("Booking not found.")).ToBeVisibleAsync();
        await Expect(Page.GetLink("Back to List")).ToBeVisibleAsync();
        await Expect(Page.GetLink("Edit Booking")).Not.ToBeVisibleAsync();

        // === Booking edit not-found ===
        await NavigateTo($"/bookings/{randomGuid}/edit");
        await Expect(Page.GetByText("Booking not found.")).ToBeVisibleAsync();
        await Expect(Page.GetLink("Back to Bookings")).ToBeVisibleAsync();

        // All not-found pages should use alert role for accessibility
        await NavigateTo($"/tours/{randomGuid}");
        await Expect(Page.Locator("[role='alert']")).ToBeVisibleAsync();
    }
}
