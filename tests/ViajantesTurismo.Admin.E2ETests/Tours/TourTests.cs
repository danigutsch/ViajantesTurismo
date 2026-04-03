using System.Globalization;

namespace ViajantesTurismo.Admin.E2ETests.Tours;

public class TourTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Can_Create_View_And_Edit_Tour()
    {
        // Arrange
        var uid = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var identifier = $"E2E{uid}";
        var initialName = $"E2E Test Tour {uid}";
        var updatedName = $"E2E Updated Tour {uid}";

        // Act
        await NavigateTo("/addtour");
        await Expect(Page).ToHaveTitleAsync("Add Tour");

        await Page.GetButton("Create Tour").ClickAsync();
        var validationSummary = Page.Locator(".validation-errors, .validation-message");
        await Expect(validationSummary.First).ToBeVisibleAsync();

        await Page.FillAsync("#identifier", identifier);
        await Page.FillAsync("#name", initialName);

        var startDate = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endDate = DateTime.UtcNow.AddDays(37).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        await Page.FillAsync("#startDate", startDate);
        await Page.FillAsync("#endDate", endDate);

        await Page.SelectOptionAsync("#currency", "Euro");
        await Page.FillAsync("#price", "1200");
        await Page.FillAsync("#singleRoom", "250");
        await Page.FillAsync("#regularBike", "80");
        await Page.FillAsync("#eBike", "150");
        await Page.FillAsync("#services", "Hotel\nBreakfast\nGuided Tour");
        await Page.FillAsync("#minCustomers", "3");
        await Page.FillAsync("#maxCustomers", "12");

        await Page.GetButton("Create Tour").ClickAsync();

        // Assert
        var successAlert = Page.Locator(".alert-success");
        await Expect(successAlert).ToBeVisibleAsync();
        await Expect(successAlert).ToContainTextAsync("Tour created successfully!");

        // Act
        await successAlert.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "View Tour Details" }).ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Tour Details");

        // Assert
        await Expect(Page.GetByText(identifier)).ToBeVisibleAsync();
        await Expect(Page.GetHeading(initialName)).ToBeVisibleAsync();
        await Expect(Page.GetByText("Hotel")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Breakfast")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Guided Tour")).ToBeVisibleAsync();

        var detailUrl = Page.Url;
        var detailUrlSegments = detailUrl.Split('/');
        var tourId = detailUrlSegments[^1];

        // Act
        await NavigateTo("/tours");
        await Expect(Page).ToHaveTitleAsync("Tours");

        // Assert
        await Expect(Page.GetByText(initialName).First).ToBeVisibleAsync();
        await Expect(Page.GetByText(identifier).First).ToBeVisibleAsync();

        // Act
        await NavigateTo($"/edittour/{tourId}");
        await Expect(Page).ToHaveTitleAsync("Edit Tour");

        await Page.FillAsync("#name", "");
        await Page.FillAsync("#name", updatedName);

        await Page.FillAsync("#services", "Hotel\nBreakfast\nGuided Tour\nBike Rental");

        await Page.GetButton("Update Tour").ClickAsync();

        var editSuccess = Page.Locator(".alert-success");
        await Expect(editSuccess).ToBeVisibleAsync();
        await Expect(editSuccess).ToContainTextAsync("Tour updated successfully!");

        await Page.CancelTimedRedirect();

        // Assert
        await NavigateTo($"/tours/{tourId}");
        await Expect(Page).ToHaveTitleAsync("Tour Details");
        await Expect(Page.GetHeading(updatedName)).ToBeVisibleAsync();
        await Expect(Page.GetByText("Bike Rental")).ToBeVisibleAsync();

        await Page.ReloadAsync();
        await Expect(Page).ToHaveTitleAsync("Tour Details");
        await Expect(Page.GetHeading(updatedName)).ToBeVisibleAsync();
        await Expect(Page.GetByText("Bike Rental")).ToBeVisibleAsync();
        await Expect(Page.GetByText(identifier)).ToBeVisibleAsync();
    }
}
