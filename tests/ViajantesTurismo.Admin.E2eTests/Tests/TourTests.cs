using Microsoft.Playwright;

namespace ViajantesTurismo.Admin.E2ETests.Tests;

public class TourTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Can_Create_View_And_Edit_Tour()
    {
        // === Validation presence: submit empty form first ===
        await NavigateToAsync("/addtour");
        await Expect(Page).ToHaveTitleAsync("Add Tour");

        await Page.GetButton("Create Tour").ClickAsync();
        var validationSummary = Page.Locator(".validation-errors, .validation-message");
        await Expect(validationSummary.First).ToBeVisibleAsync();

        // === Add Tour: fill valid form ===
        await Page.FillAsync("#identifier", "E2ETST");
        await Page.FillAsync("#name", "E2E Test Tour");

        var startDate = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.AddDays(37).ToString("yyyy-MM-dd");
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

        // === Success alert appears ===
        var successAlert = Page.Locator(".alert-success");
        await Expect(successAlert).ToBeVisibleAsync();
        await Expect(successAlert).ToContainTextAsync("Tour created successfully!");

        // === Navigate to tour details via success link ===
        await successAlert.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "View Tour Details" }).ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Tour Details");

        // Verify details fields
        await Expect(Page.GetByText("E2ETST")).ToBeVisibleAsync();
        await Expect(Page.GetHeading("E2E Test Tour")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Hotel")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Breakfast")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Guided Tour")).ToBeVisibleAsync();

        // Capture the detail page URL to extract tour ID
        var detailUrl = Page.Url;
        var tourId = detailUrl.Split('/').Last();

        // === Tour appears in list ===
        await NavigateToAsync("/tours");
        await Expect(Page).ToHaveTitleAsync("Tours");
        await Expect(Page.GetByText("E2E Test Tour").First).ToBeVisibleAsync();
        await Expect(Page.GetByText("E2ETST").First).ToBeVisibleAsync();

        // === Edit Tour ===
        await NavigateToAsync($"/edittour/{tourId}");
        await Expect(Page).ToHaveTitleAsync("Edit Tour");

        await Page.FillAsync("#name", "");
        await Page.FillAsync("#name", "E2E Updated Tour");

        await Page.FillAsync("#services", "Hotel\nBreakfast\nGuided Tour\nBike Rental");

        await Page.GetButton("Update Tour").ClickAsync();

        // Success and redirect
        var editSuccess = Page.Locator(".alert-success");
        await Expect(editSuccess).ToBeVisibleAsync();
        await Expect(editSuccess).ToContainTextAsync("Tour updated successfully!");

        // Cancel auto-redirect to verify details manually
        var cancelButton = Page.Locator(".alert-info button", new PageLocatorOptions { HasText = "Cancel" });
        if (await cancelButton.CountAsync() > 0)
        {
            await cancelButton.ClickAsync();
        }

        // === Verify via details page ===
        await NavigateToAsync($"/tours/{tourId}");
        await Expect(Page).ToHaveTitleAsync("Tour Details");
        await Expect(Page.GetHeading("E2E Updated Tour")).ToBeVisibleAsync();
        await Expect(Page.GetByText("Bike Rental")).ToBeVisibleAsync();
    }
}
