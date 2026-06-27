namespace ViajantesTurismo.Admin.SystemTests.Shared;

public class NotFoundPageTests(AspireSystemTestFixture fixture) : AspireSystemTestBase<AspireSystemTestFixture>(fixture)
{
    [Fact]
    public async Task Not_found_pages_hide_action_links_and_show_consistent_layout()
    {
        // Arrange
        var randomGuid = Guid.NewGuid().ToString();

        // Act
        // Assert
        await NotFoundPageTestHelpers.AssertNotFoundPageWithHiddenAction(Page, NavigateTo, $"/tours/{randomGuid}", "Tour not found.", "Back to Tours", "Edit Tour");
        await NotFoundPageTestHelpers.AssertNotFoundPage(Page, NavigateTo, $"/edittour/{randomGuid}", "Tour not found.", "Back to Tours");
        await NotFoundPageTestHelpers.AssertNotFoundPageWithHiddenAction(Page, NavigateTo, $"/customers/{randomGuid}", "Customer not found.", "Back to Customers", "Edit Customer");
        await NotFoundPageTestHelpers.AssertNotFoundPage(Page, NavigateTo, $"/customers/{randomGuid}/edit", "Customer not found.", "Back to Customers");
        await NotFoundPageTestHelpers.AssertNotFoundPageWithHiddenAction(Page, NavigateTo, $"/bookings/{randomGuid}", "Booking not found.", "Back to List", "Edit Booking");
        await NotFoundPageTestHelpers.AssertNotFoundPage(Page, NavigateTo, $"/bookings/{randomGuid}/edit", "Booking not found.", "Back to Bookings");

        await NavigateTo($"/tours/{randomGuid}");
        await Expect(Page.Locator("[role='alert']")).ToBeVisibleAsync();
    }
}
