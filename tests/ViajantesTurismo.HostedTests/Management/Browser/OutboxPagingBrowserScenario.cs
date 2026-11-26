using System.Net;
using Microsoft.AspNetCore.WebUtilities;
using SharedKernel.Testing.Http;

namespace ViajantesTurismo.HostedTests.Management.Browser;

/// <summary>Returns the first outbox page and holds the continuation before returning an unavailable response.</summary>
internal sealed class OutboxPagingBrowserScenario : HttpMessageHandler
{
    private const string StorageId = "22222222-2222-2222-2222-222222222222";
    private const string FirstPage = $$"""
        {
          "rows":[{"storageId":"{{StorageId}}","eventId":"synthetic-pending-publication",
            "eventType":"TourChanged","recordedAt":"2026-11-20T08:00:00Z",
            "attempts":0,"lastAttemptAt":null,"nextAttemptAt":null}],
          "nextRecordedAt":"2026-11-20T08:00:00Z","nextStorageId":"{{StorageId}}"
        }
        """;

    private readonly TaskCompletionSource _continuationAllowed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Releases the held continuation.</summary>
    public void AllowContinuation() => _continuationAllowed.TrySetResult();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var query = QueryHelpers.ParseQuery(request.RequestUri?.Query);
        if (!query.TryGetValue("afterStorageId", out var storageId))
        {
            return HttpResponseFactory.Json(FirstPage, HttpStatusCode.OK);
        }

        if (!string.Equals(storageId.ToString(), StorageId, StringComparison.Ordinal)
            || !string.Equals(query["afterRecordedAt"].ToString(), "2026-11-20T08:00:00.0000000+00:00", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The browser requested an unexpected outbox cursor.");
        }

        await _continuationAllowed.Task.WaitAsync(cancellationToken);
        return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            AllowContinuation();
        }

        base.Dispose(disposing);
    }
}
