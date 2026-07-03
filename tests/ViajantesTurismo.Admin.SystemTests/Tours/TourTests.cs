using System.Globalization;

namespace ViajantesTurismo.Admin.SystemTests.Tours;

public class TourTests(AspireSystemTestFixture fixture) : AspireSystemTestBase<AspireSystemTestFixture>(fixture)
{
    [Fact]
    public async Task Can_create_view_and_edit_tour()
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

        await Page.Locator("#identifier").FillAndExpectValue(identifier);
        await Page.Locator("#name").FillAndExpectValue(initialName);

        var startDate = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endDate = DateTime.UtcNow.AddDays(37).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        await Page.Locator("#startDate").FillAndExpectValue(startDate);
        await Page.Locator("#endDate").FillAndExpectValue(endDate);

        await Page.SelectOptionAsync("#currency", "Euro");
        await Page.Locator("#price").FillAndExpectValue("1200");
        await Page.Locator("#singleRoom").FillAndExpectValue("250");
        await Page.Locator("#regularBike").FillAndExpectValue("80");
        await Page.Locator("#eBike").FillAndExpectValue("150");
        await Page.Locator("#services").FillAndExpectValue("Hotel\nBreakfast\nGuided Tour");
        await Page.Locator("#minCustomers").FillAndExpectValue("3");
        await Page.Locator("#maxCustomers").FillAndExpectValue("12");

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

        await Page.Locator("#name").FillAndExpectValue("");
        await Page.Locator("#name").FillAndExpectValue(updatedName);

        await Page.Locator("#services").FillAndExpectValue("Hotel\nBreakfast\nGuided Tour\nBike Rental");

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
