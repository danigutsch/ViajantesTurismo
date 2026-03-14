namespace ViajantesTurismo.Admin.E2ETests.Shared;

public class NotFoundErrorTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Invalid_GUID_Format_Shows_404_Page()
    {
        // Arrange
        var invalidGuids = new[] { "invalid-guid", "not-a-guid", "12345", "abc" };

        foreach (var invalidGuid in invalidGuids)
        {
            // Act
            await NavigateTo($"/tours/{invalidGuid}");

            // Assert
            await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Page Not Found" })).ToBeVisibleAsync();
            await Expect(Page.GetByText("does not exist or the URL is invalid")).ToBeVisibleAsync();
        }

        // Act
        await NavigateTo("/bookings/not-a-guid");

        // Assert
        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Page Not Found" })).ToBeVisibleAsync();

        // Act
        await NavigateTo("/customers/not-a-guid");

        // Assert
        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Page Not Found" })).ToBeVisibleAsync();

        // Act
        await NavigateTo("/customers/not-a-guid/edit");

        // Assert
        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Page Not Found" })).ToBeVisibleAsync();
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
