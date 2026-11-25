using SharedKernel.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Contracts.Http;

namespace ViajantesTurismo.HostedTests.Management.Browser;

/// <summary>Provides two draft history pages and holds the earlier request while Chromium observes loading.</summary>
internal sealed class DocumentMaintenanceTimelineBrowserScenario : IDocumentMaintenanceTimelineApiClient, IDisposable
{
    private static readonly DocumentMaintenanceRunCursorDto FirstPosition = new(
        new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero), Guid.CreateVersion7());
    private readonly TaskCompletionSource _earlierAllowed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <inheritdoc />
    public async Task<HttpOutcome<GetDocumentMaintenanceTimelineDto>> Read(
        DocumentMaintenanceJob job, DocumentMaintenanceRunCursorDto? before, CancellationToken ct)
    {
        if (job != DocumentMaintenanceJob.Drafts || (before is not null && before != FirstPosition))
        {
            throw new InvalidOperationException("The browser requested an unexpected maintenance job or cursor.");
        }

        if (before is not null)
        {
            await _earlierAllowed.Task.WaitAsync(ct);
        }

        var time = before is null ? FirstPosition.OccurredAtUtc : FirstPosition.OccurredAtUtc.AddHours(-4);
        return new HttpOutcome<GetDocumentMaintenanceTimelineDto>
        {
            Kind = HttpOutcomeKind.Succeeded,
            Value = new GetDocumentMaintenanceTimelineDto
            {
                Runs = [new(time.UtcDateTime, 2, 1, "Succeeded")],
                Next = before is null ? FirstPosition : null,
            },
        };
    }

    /// <summary>Completes pending requests for the earlier page.</summary>
    public void AllowEarlierPage() => _earlierAllowed.TrySetResult();

    /// <inheritdoc />
    public void Dispose() => AllowEarlierPage();
}
