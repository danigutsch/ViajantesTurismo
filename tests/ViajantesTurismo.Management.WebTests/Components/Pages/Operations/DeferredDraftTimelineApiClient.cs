using SharedKernel.AspNetCore.Authentication.Bff;
using SharedKernel.HttpClients;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Operations;

/// <summary>Holds the first timeline request until the test changes jobs.</summary>
internal sealed class DeferredDraftTimelineApiClient : IDocumentMaintenanceTimelineApiClient
{
    private readonly TaskCompletionSource<HttpOutcome<GetDocumentMaintenanceTimelineDto>> _drafts = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <inheritdoc />
    public Task<HttpOutcome<GetDocumentMaintenanceTimelineDto>> Read(
        DocumentMaintenanceJob job, DocumentMaintenanceRunCursorDto? before, CancellationToken ct) =>
        job == DocumentMaintenanceJob.Drafts ? _drafts.Task : Task.FromResult(Page(31));

    /// <summary>Releases the older draft request with a result or a token failure.</summary>
    /// <param name="tokenUnavailable">Whether the older request loses its user session.</param>
    public void CompleteDrafts(bool tokenUnavailable)
    {
        if (tokenUnavailable)
        {
            _drafts.SetException(new UserTokenUnavailableException("synthetic-stale-session"));
            return;
        }

        _drafts.SetResult(Page(42));
    }

    private static HttpOutcome<GetDocumentMaintenanceTimelineDto> Page(int count) => new()
    {
        Kind = HttpOutcomeKind.Succeeded,
        Value = new GetDocumentMaintenanceTimelineDto
        {
            Runs = [new(new DateTime(2026, 11, 24, 8, 0, 0, DateTimeKind.Utc), count, 0, "Succeeded")],
        },
    };
}
