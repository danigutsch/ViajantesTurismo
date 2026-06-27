namespace ViajantesTurismo.Admin.SystemTests.Shared;

public class NotFoundErrorTests(AspireSystemTestFixture fixture) : AspireSystemTestBase<AspireSystemTestFixture>(fixture)
{
    [Fact]
    public async Task Invalid_GUID_format_shows_404_page()
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
    public async Task Non_existent_route_shows_custom_404_page()
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
