namespace ViajantesTurismo.Admin.E2ETests.Shared;

public class NotFoundPageTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Not_Found_Pages_Hide_Action_Links_And_Show_Consistent_Layout()
    {
        // Arrange
        var randomGuid = Guid.NewGuid().ToString();

        // Act
        // Assert
        await AssertNotFoundPageWithHiddenAction($"/tours/{randomGuid}", "Tour not found.", "Back to Tours", "Edit Tour");
        await AssertNotFoundPage($"/edittour/{randomGuid}", "Tour not found.", "Back to Tours");
        await AssertNotFoundPageWithHiddenAction($"/customers/{randomGuid}", "Customer not found.", "Back to Customers", "Edit Customer");
        await AssertNotFoundPage($"/customers/{randomGuid}/edit", "Customer not found.", "Back to Customers");
        await AssertNotFoundPageWithHiddenAction($"/bookings/{randomGuid}", "Booking not found.", "Back to List", "Edit Booking");
        await AssertNotFoundPage($"/bookings/{randomGuid}/edit", "Booking not found.", "Back to Bookings");

        await NavigateTo($"/tours/{randomGuid}");
        await Expect(Page.Locator("[role='alert']")).ToBeVisibleAsync();
    }

    private async Task AssertNotFoundPage(string path, string message, string backLink)
    {
        await NavigateTo(path);
        await Expect(Page.GetByText(message)).ToBeVisibleAsync();
        await Expect(Page.GetLink(backLink)).ToBeVisibleAsync();
    }

    private async Task AssertNotFoundPageWithHiddenAction(string path, string message, string backLink, string hiddenActionLink)
    {
        await AssertNotFoundPage(path, message, backLink);
        await Expect(Page.GetLink(hiddenActionLink)).Not.ToBeVisibleAsync();
    }
}
