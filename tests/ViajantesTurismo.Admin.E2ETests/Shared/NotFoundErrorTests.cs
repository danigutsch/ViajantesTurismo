namespace ViajantesTurismo.Admin.E2ETests.Shared;

public class NotFoundErrorTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Invalid_GUID_Format_Shows_404_Page()
    {
        // Arrange
        const string invalidGuid = "not-a-guid";

        // Act
        await NavigateTo($"/tours/{invalidGuid}");

        // Assert
        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Page Not Found" })).ToBeVisibleAsync();
        await Expect(Page.GetByText("does not exist or the URL is invalid")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Non_Existent_Route_Shows_Custom_404_Page()
    {
        // Arrange

        // Act
        await NavigateTo("/nonexistent-page");

        // Assert
        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Page Not Found" })).ToBeVisibleAsync();
        await Expect(Page.GetByText("does not exist or the URL is invalid")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Back to Dashboard" })).ToBeVisibleAsync();

        // Act
        await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Back to Dashboard" }).ClickAsync();

        // Assert
        await Expect(Page.GetHeading("ViajantesTurismo Admin Dashboard")).ToBeVisibleAsync();
    }
}
