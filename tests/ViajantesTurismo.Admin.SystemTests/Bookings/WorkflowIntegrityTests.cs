namespace ViajantesTurismo.Admin.SystemTests.Bookings;

public class WorkflowIntegrityTests(AspireSystemTestFixture fixture) : AspireSystemTestBase<AspireSystemTestFixture>(fixture)
{
    [Fact]
    public async Task Tour_Details_Add_Booking_Flow_Should_Validate_Without_Leaking_Internal_Errors()
    {
        // Arrange
        var tour = await ApiClient.CreateTour();

        // Act
        await NavigateTo($"/tours/{tour.Id}");
        await Expect(Page).ToHaveTitleAsync("Tour Details");

        await Page.GetButton("Add Booking").ClickAsync();
        await Expect(Page.GetButton("Create Booking")).ToBeVisibleAsync();
        await Page.GetButton("Create Booking").ClickAsync();

        var alert = Page.GetByRole(AriaRole.Alert);
        var inlineValidation = Page.Locator(".validation-message");
        var feedbackLocator = Page.Locator("[role='alert'], .validation-message");
        await Expect(feedbackLocator.First).ToBeVisibleAsync();

        // Assert
        var hasFriendlyValidationFeedback = await WorkflowIntegrityTestHelpers.HasFriendlyValidationFeedback(alert, inlineValidation);
        Assert.True(hasFriendlyValidationFeedback,
            "Expected user-facing validation feedback (e.g., required-field or friendly validation message). ");

        await Expect(Page).ToHaveTitleAsync("Tour Details");
        await WorkflowIntegrityTestHelpers.AssertNoTechnicalLeak(Page, alert, inlineValidation);
    }
}
