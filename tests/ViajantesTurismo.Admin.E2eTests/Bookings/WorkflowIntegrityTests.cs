namespace ViajantesTurismo.Admin.E2ETests.Bookings;

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
        // Act
        await NavigateTo("/customers/create/personal-info");
        await Expect(Page).ToHaveTitleAsync("Create Customer - Personal Information");

        await Page.Locator("button[type='submit']").First.ClickAsync();

        // Assert
        var nationalityValidation = Page.Locator(".validation-message", new PageLocatorOptions { HasText = "Nationality is required" });
        await Expect(nationalityValidation.First).ToBeVisibleAsync();

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
        var hasFriendlyValidationFeedback = await HasFriendlyValidationFeedback(alert, inlineValidation);
        Assert.True(hasFriendlyValidationFeedback,
            "Expected user-facing validation feedback (e.g., required-field or friendly validation message). ");

        await Expect(Page).ToHaveTitleAsync("Tour Details");
        await AssertNoTechnicalLeak(alert, inlineValidation);
    }

    [Fact]
    public async Task Customer_Create_And_Edit_Flows_Should_Only_Render_Contextual_Inputs()
    {
        const string leakedSearchPlaceholder = "Search customers by name or email...";

        // Arrange
        var customer = await ApiClient.CreateCustomer();

        // Act
        // Assert
        await NavigateTo("/customers/create/accommodation");
        await Expect(Page.Locator($"input[placeholder='{leakedSearchPlaceholder}']")).ToHaveCountAsync(0);

        await NavigateTo($"/customers/{customer.Id}/edit");
        await Expect(Page.Locator($"input[placeholder='{leakedSearchPlaceholder}']")).ToHaveCountAsync(0);
    }

    private static async Task<bool> HasFriendlyValidationFeedback(ILocator alert, ILocator inlineValidation)
    {
        var feedbackTexts = await CollectFeedbackTexts(alert, inlineValidation);
        return feedbackTexts.Any(text =>
            FriendlyValidationIndicators.Any(indicator =>
                text.Contains(indicator, StringComparison.OrdinalIgnoreCase)));
    }

    private async Task AssertNoTechnicalLeak(ILocator alert, ILocator inlineValidation)
    {
        var feedbackTexts = await CollectFeedbackTexts(alert, inlineValidation);
        var textsToInspect = feedbackTexts.Count == 0
            ? [await ReadBodyText()]
            : feedbackTexts;

        foreach (var text in textsToInspect)
        {
            foreach (var marker in TechnicalLeakIndicators)
            {
                Assert.DoesNotContain(marker, text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    private static async Task<List<string>> CollectFeedbackTexts(ILocator alert, ILocator inlineValidation)
    {
        var feedbackTexts = new List<string>();
        await CollectTexts(alert, feedbackTexts);
        await CollectTexts(inlineValidation, feedbackTexts);
        return feedbackTexts;
    }

    private static async Task CollectTexts(ILocator locator, List<string> feedbackTexts)
    {
        var count = await locator.CountAsync();
        for (var i = 0; i < count; i++)
        {
            var element = locator.Nth(i);
            await element.WaitForAsync();
            feedbackTexts.Add(await element.InnerTextAsync());
        }
    }

    private async Task<string> ReadBodyText()
    {
        await Page.Locator("body").WaitForAsync();
        return await Page.Locator("body").InnerTextAsync();
    }
}
