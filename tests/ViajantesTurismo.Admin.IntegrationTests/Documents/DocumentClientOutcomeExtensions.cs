using SharedKernel.HttpClients;

namespace ViajantesTurismo.Admin.IntegrationTests.Documents;

internal static class DocumentClientOutcomeExtensions
{
    public static async Task<TValue> RequireSucceededValue<TValue>(
        this Task<HttpOutcome<TValue>> outcomeTask)
        where TValue : class
    {
        ArgumentNullException.ThrowIfNull(outcomeTask);

        var outcome = await outcomeTask.ConfigureAwait(false);
        outcome.Kind.ShouldBe(ContractCommandOutcomeKind.Succeeded);
        return outcome.Value.ShouldNotBeNull();
    }
}
