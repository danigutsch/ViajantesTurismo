namespace ViajantesTurismo.Admin.SystemTests.Bookings;

public static class WorkflowIntegrityTestHelpers
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

    public static async Task<bool> HasFriendlyValidationFeedback(ILocator alert, ILocator inlineValidation)
    {
        ArgumentNullException.ThrowIfNull(alert);
        ArgumentNullException.ThrowIfNull(inlineValidation);

        var feedbackTexts = await CollectFeedbackTexts(alert, inlineValidation);
        return feedbackTexts.Any(text =>
            FriendlyValidationIndicators.Any(indicator =>
                text.Contains(indicator, StringComparison.OrdinalIgnoreCase)));
    }

    public static async Task AssertNoTechnicalLeak(IPage page, ILocator alert, ILocator inlineValidation)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(alert);
        ArgumentNullException.ThrowIfNull(inlineValidation);

        var feedbackTexts = await CollectFeedbackTexts(alert, inlineValidation);
        var textsToInspect = feedbackTexts.Count == 0
            ? [await ReadBodyText(page)]
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

    private static async Task<string> ReadBodyText(IPage page)
    {
        await page.Locator("body").WaitForAsync();
        return await page.Locator("body").InnerTextAsync();
    }
}
