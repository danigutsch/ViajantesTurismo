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
        await NavigateToAsync("/tours");
        await Page.Locator("table tbody tr").First.GetLink("View").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Tour Details");

        await Page.GetButton("Add Booking").ClickAsync();
        await Expect(Page.GetButton("Create Booking")).ToBeVisibleAsync();

        // Empty submit should produce friendly validation, not internal exception details.
        await Page.GetButton("Create Booking").ClickAsync();

        // Feedback channel may vary (toast or inline validation messages);
        // enforce the real contract: show user-facing validation and never leak technical details.
        var alert = Page.GetByRole(AriaRole.Alert);
        var inlineValidation = Page.Locator(".validation-message");

        var feedbackDetected = false;
        for (var i = 0; i < 20; i++)
        {
            if (await alert.CountAsync() > 0 || await inlineValidation.CountAsync() > 0)
            {
                feedbackDetected = true;
                break;
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        Assert.True(feedbackDetected,
            "Expected explicit user-facing validation feedback after empty submit.");

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

        await NavigateToAsync("/customers/create/accommodation");
        await Expect(Page.Locator($"input[placeholder='{leakedSearchPlaceholder}']")).ToHaveCountAsync(0);

        await NavigateToAsync("/customers");
        var editHref = await Page.Locator("table tbody tr").First
            .GetLink("Edit").GetAttributeAsync("href");

        await NavigateToAsync(editHref!);
        await Expect(Page.Locator($"input[placeholder='{leakedSearchPlaceholder}']")).ToHaveCountAsync(0);
    }

    private static async Task<bool> HasFriendlyValidationFeedbackAsync(ILocator alert, ILocator inlineValidation)
    {
        if (await inlineValidation.CountAsync() > 0)
        {
            for (var i = 0; i < await inlineValidation.CountAsync(); i++)
            {
                var text = await inlineValidation.Nth(i).InnerTextAsync();
                if (FriendlyValidationIndicators.Any(indicator =>
                        text.Contains(indicator, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }
        }

        if (await alert.CountAsync() > 0)
        {
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
            feedbackTexts.Add(await alert.Nth(i).InnerTextAsync());
        }

        var validationCount = await inlineValidation.CountAsync();
        for (var i = 0; i < validationCount; i++)
        {
            feedbackTexts.Add(await inlineValidation.Nth(i).InnerTextAsync());
        }

        if (feedbackTexts.Count == 0)
        {
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
