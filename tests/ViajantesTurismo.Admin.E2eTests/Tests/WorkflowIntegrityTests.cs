using Microsoft.Playwright;

namespace ViajantesTurismo.Admin.E2ETests.Tests;

public class WorkflowIntegrityTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    private static readonly string[] TechnicalLeakIndicators =
    [
        "exception",
        "stack trace",
        "object reference not set",
        "nullable object must have a value",
        "inner exception",
        "parameter name:"
    ];

    private static readonly string[] FriendlyValidationIndicators =
    [
        "validation failed",
        "please check the form",
        "required"
    ];

    [Fact]
    public async Task Customer_Creation_Flow_Should_Expose_Controls_For_All_Required_Personal_Fields()
    {
        await NavigateToAsync("/customers/create/personal-info");
        await Expect(Page).ToHaveTitleAsync("Create Customer - Personal Information");

        // Trigger validation so required messages become explicit in UI.
        await Page.Locator("button[type='submit']").First.ClickAsync();

        // Same validation text may be rendered in both summary and field-level containers.
        // Use a non-strict assertion by checking at least one matching validation message.
        var nationalityValidation = Page.Locator(".validation-message", new PageLocatorOptions { HasText = "Nationality is required" });
        await Expect(nationalityValidation.First).ToBeVisibleAsync();

        // Guardrail: every required field should have a corresponding input control.
        await Expect(Page.Locator("#firstName, input[name='_model.FirstName']")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#lastName, input[name='_model.LastName']")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#birthDate, input[name='_model.BirthDate']")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#gender, select[name='_model.Gender']")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#nationality, input[name='_model.Nationality']")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#occupation, input[name='_model.Occupation']")).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task Tour_Details_Add_Booking_Flow_Should_Validate_Without_Leaking_Internal_Errors()
    {
        // Arrange
        var tour = await ApiTestHelper.CreateTourAsync(ApiClient);

        await NavigateToAsync($"/tours/{tour.Id}");
        await Expect(Page).ToHaveTitleAsync("Tour Details");

        await Page.GetButton("Add Booking").ClickAsync();
        await Expect(Page.GetButton("Create Booking")).ToBeVisibleAsync();

        // Empty submit should produce friendly validation, not internal exception details.
        await Page.GetButton("Create Booking").ClickAsync();

        // Wait for validation feedback to appear using Playwright's built-in waiting
        var alert = Page.GetByRole(AriaRole.Alert);
        var inlineValidation = Page.Locator(".validation-message");
        var feedbackLocator = Page.Locator("[role='alert'], .validation-message");
        await Expect(feedbackLocator.First).ToBeVisibleAsync();

        var hasFriendlyValidationFeedback = await HasFriendlyValidationFeedbackAsync(alert, inlineValidation);
        Assert.True(hasFriendlyValidationFeedback,
            "Expected user-facing validation feedback (e.g., required-field or friendly validation message). ");

        await Expect(Page).ToHaveTitleAsync("Tour Details");
        await AssertNoTechnicalLeakAsync(alert, inlineValidation);
    }

    [Fact]
    public async Task Customer_Create_And_Edit_Flows_Should_Only_Render_Contextual_Inputs()
    {
        const string leakedSearchPlaceholder = "Search customers by name or email...";

        // Arrange
        var customer = await ApiTestHelper.CreateCustomerAsync(ApiClient);

        await NavigateToAsync("/customers/create/accommodation");
        await Expect(Page.Locator($"input[placeholder='{leakedSearchPlaceholder}']")).ToHaveCountAsync(0);

        await NavigateToAsync($"/customers/{customer.Id}/edit");
        await Expect(Page.Locator($"input[placeholder='{leakedSearchPlaceholder}']")).ToHaveCountAsync(0);
    }

    private static async Task<bool> HasFriendlyValidationFeedbackAsync(ILocator alert, ILocator inlineValidation)
    {
        var validationCount = await inlineValidation.CountAsync();
        if (validationCount > 0)
        {
            for (var i = 0; i < validationCount; i++)
            {
                var element = inlineValidation.Nth(i);
                await element.WaitForAsync();
                var text = await element.InnerTextAsync();
                if (FriendlyValidationIndicators.Any(indicator =>
                        text.Contains(indicator, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }
        }

        var alertCount = await alert.CountAsync();
        if (alertCount > 0)
        {
            await alert.First.WaitForAsync();
            var alertText = await alert.First.InnerTextAsync();
            return FriendlyValidationIndicators.Any(indicator =>
                alertText.Contains(indicator, StringComparison.OrdinalIgnoreCase));
        }

        return false;
    }

    private async Task AssertNoTechnicalLeakAsync(ILocator alert, ILocator inlineValidation)
    {
        var feedbackTexts = new List<string>();

        var alertCount = await alert.CountAsync();
        for (var i = 0; i < alertCount; i++)
        {
            var element = alert.Nth(i);
            await element.WaitForAsync();
            feedbackTexts.Add(await element.InnerTextAsync());
        }

        var validationCount = await inlineValidation.CountAsync();
        for (var i = 0; i < validationCount; i++)
        {
            var element = inlineValidation.Nth(i);
            await element.WaitForAsync();
            feedbackTexts.Add(await element.InnerTextAsync());
        }

        if (feedbackTexts.Count == 0)
        {
            await Page.Locator("body").WaitForAsync();
            feedbackTexts.Add(await Page.Locator("body").InnerTextAsync());
        }

        foreach (var text in feedbackTexts)
        {
            foreach (var marker in TechnicalLeakIndicators)
            {
                Assert.DoesNotContain(marker, text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
