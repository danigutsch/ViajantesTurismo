using PandaBear.Testing;
using Microsoft.Playwright;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Bases;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Fixtures;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Helpers;
using ViajantesTurismo.Admin.Testing.Integration.Helpers;
using PandaBear.Testing.Playwright;
namespace ViajantesTurismo.HostedTests.Admin.System.Bookings;

[TestScope(TestScope.System)]
public class WorkflowIntegrityTests(AspireSystemTestFixture fixture) : AspireSystemTestBase<AspireSystemTestFixture>(fixture)
{
    [Fact]
    public async Task Tour_details_add_booking_flow_should_validate_without_leaking_internal_errors()
    {
        // Arrange
        var tour = await ApiClient.CreateTour();

        // Act
        await NavigateTo($"/tours/{tour.Id}");
        await Page.ShouldHaveTitle("Tour Details");

        await Page.GetButton("Add Booking").ClickAsync();
        await Expect(Page.GetButton("Create Booking")).ToBeVisibleAsync();
        await Page.GetButton("Create Booking").ClickAsync();

        var alert = Page.GetByRole(AriaRole.Alert);
        var inlineValidation = Page.Locator(".validation-message");
        var feedbackLocator = Page.Locator("[role='alert'], .validation-message");
        await Expect(feedbackLocator.First).ToBeVisibleAsync();

        // Assert
        var hasFriendlyValidationFeedback = await WorkflowIntegrityTestHelpers.HasFriendlyValidationFeedback(alert, inlineValidation);
        hasFriendlyValidationFeedback.ShouldBeTrue("Expected user-facing validation feedback (e.g., required-field or friendly validation message). ");

        await Page.ShouldHaveTitle("Tour Details");
        await WorkflowIntegrityTestHelpers.AssertNoTechnicalLeak(Page, alert, inlineValidation);
    }
}
