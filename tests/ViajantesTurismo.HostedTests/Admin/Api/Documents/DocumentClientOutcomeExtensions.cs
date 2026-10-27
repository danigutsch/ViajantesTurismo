using PandaBear.HttpClients;

namespace ViajantesTurismo.Admin.IntegrationTests.Documents;

/// <summary>Test assertions for document client outcomes.</summary>
internal static class DocumentClientOutcomeExtensions
{
    /// <summary>Awaits the outcome, asserts it succeeded, and returns its non-null value.</summary>
    public static async Task<TValue> RequireSucceededValue<TValue>(
        this Task<HttpOutcome<TValue>> outcomeTask)
        where TValue : class
    {
        ArgumentNullException.ThrowIfNull(outcomeTask);

        var outcome = await outcomeTask.ConfigureAwait(false);
        outcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        return outcome.Value.ShouldNotBeNull();
    }
}
