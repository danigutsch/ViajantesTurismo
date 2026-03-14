using Microsoft.Playwright;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Bases;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Fixtures;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Helpers;

namespace ViajantesTurismo.Admin.E2ETests.Shared;

public class NotFoundErrorTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Returns_NotFound_For_Invalid_Routes_And_Guids()
    {
        var randomGuid = Guid.NewGuid().ToString();

        // /tours/{random-guid} → "Tour not found."
        await NavigateTo($"/tours/{randomGuid}");
        await Expect(Page.GetByText("Tour not found.")).ToBeVisibleAsync();

        // /customers/{random-guid} → "Customer not found."
        await NavigateTo($"/customers/{randomGuid}");
        await Expect(Page.GetByText("Customer not found.")).ToBeVisibleAsync();

        // /edittour/{random-guid} → "Tour not found."
        await NavigateTo($"/edittour/{randomGuid}");
        await Expect(Page.GetByText("Tour not found.")).ToBeVisibleAsync();

        // /bookings/{random-guid} → "Booking not found." with no "Edit Booking" link
        await NavigateTo($"/bookings/{randomGuid}");
        await Expect(Page.GetByText("Booking not found.")).ToBeVisibleAsync();
        await Expect(Page.GetLink("Back to List")).ToBeVisibleAsync();
        await Expect(Page.GetLink("Edit Booking")).Not.ToBeVisibleAsync();

        // /bookings/{random-guid}/edit → similar error
        await NavigateTo($"/bookings/{randomGuid}/edit");
        await Expect(Page.GetByRole(AriaRole.Alert)).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Invalid_GUID_Format_Shows_404_Page()
    {
        var invalidGuids = new[] { "invalid-guid", "not-a-guid", "12345", "abc" };

        foreach (var invalidGuid in invalidGuids)
        {
            await NavigateTo($"/tours/{invalidGuid}");
            await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Page Not Found" })).ToBeVisibleAsync();
            await Expect(Page.GetByText("does not exist or the URL is invalid")).ToBeVisibleAsync();
        }

        // Also verify bookings and customers with invalid GUIDs
        await NavigateTo("/bookings/not-a-guid");
        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Page Not Found" })).ToBeVisibleAsync();

        await NavigateTo("/customers/not-a-guid");
        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Page Not Found" })).ToBeVisibleAsync();

        await NavigateTo("/customers/not-a-guid/edit");
        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Page Not Found" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Non_Existent_Route_Shows_Custom_404_Page()
    {
        await NavigateTo("/nonexistent-page");
        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Page Not Found" })).ToBeVisibleAsync();
        await Expect(Page.GetByText("does not exist or the URL is invalid")).ToBeVisibleAsync();

        // Should have a link back to the dashboard
        await Expect(Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Back to Dashboard" })).ToBeVisibleAsync();

        // Clicking the link should navigate to the dashboard
        await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Back to Dashboard" }).ClickAsync();
        await Expect(Page.GetHeading("ViajantesTurismo Admin Dashboard")).ToBeVisibleAsync();
    }
}
