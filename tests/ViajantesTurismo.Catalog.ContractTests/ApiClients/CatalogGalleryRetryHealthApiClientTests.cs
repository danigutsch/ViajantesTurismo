using System.Net;
using SharedKernel.HttpClients;
using SharedKernel.Testing;
using SharedKernel.Testing.Contracts;
using ViajantesTurismo.Catalog.Contracts.Http;

namespace ViajantesTurismo.Catalog.ContractTests.ApiClients;

/// <summary>Verifies the Catalog Gallery retry summary HTTP contract.</summary>
[Trait(TestTraitNames.CategoryName, TestTraitValues.ContractCategory)]
[TestScope(TestScope.Component)]
public sealed class CatalogGalleryRetryHealthApiClientTests
{
    [Theory]
    [InlineData(0, "null")]
    [InlineData(2, "\"2026-11-25T10:30:00Z\"")]
    public async Task Reads_the_count_and_oldest_due_time_from_the_catalog_operations_route(long count, string timestamp)
    {
        // Arrange
        var body = FormattableString.Invariant($$"""
            {"duePlacementCount":{{count}},"oldestDueAt":{{timestamp}}}
            """);
        HttpMethod? method = null;
        string? path = null;
        using var client = ContractHttpClientTestHelper.CreateClient(request =>
        {
            method = request.Method;
            path = request.RequestUri?.PathAndQuery;
            return ContractHttpClientTestHelper.JsonResponse(body);
        });
        var sut = new CatalogGalleryOperationsApiClient(client);
        DateTimeOffset? expected = count == 0 ? null : new DateTimeOffset(2026, 11, 25, 10, 30, 0, TimeSpan.Zero);

        // Act
        var outcome = await sut.GetRetryHealth(TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        var summary = outcome.Value.ShouldNotBeNull();
        summary.DuePlacementCount.ShouldBe(count);
        summary.OldestDueAt.ShouldBe(expected);
        method.ShouldBe(HttpMethod.Get);
        path.ShouldBe("/api/v1/catalog/operations/gallery/retry-health");
    }

    [Theory]
    [InlineData("""{"duePlacementCount":0}""")]
    [InlineData("""{"oldestDueAt":null}""")]
    [InlineData("""{"duePlacementCount":-1,"oldestDueAt":null}""")]
    [InlineData("""{"duePlacementCount":2,"oldestDueAt":null}""")]
    [InlineData("""{"duePlacementCount":0,"oldestDueAt":"2026-11-25T10:30:00Z"}""")]
    public async Task Rejects_missing_or_inconsistent_retry_observations(string body)
    {
        // Arrange
        using var client = ContractHttpClientTestHelper.CreateClient(_ => ContractHttpClientTestHelper.JsonResponse(body));
        var sut = new CatalogGalleryOperationsApiClient(client);

        // Act
        var outcome = await sut.GetRetryHealth(TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.MalformedBody);
        outcome.Value.ShouldBeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, HttpOutcomeKind.NotFound)]
    [InlineData(HttpStatusCode.Forbidden, HttpOutcomeKind.Forbidden)]
    [InlineData(HttpStatusCode.ServiceUnavailable, HttpOutcomeKind.UnexpectedStatus)]
    public async Task Retains_HTTP_failure_outcomes_without_an_empty_health_snapshot(HttpStatusCode status, HttpOutcomeKind expected)
    {
        // Arrange
        using var client = ContractHttpClientTestHelper.CreateClient(_ => new HttpResponseMessage(status));
        var sut = new CatalogGalleryOperationsApiClient(client);

        // Act
        var outcome = await sut.GetRetryHealth(TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(expected);
        outcome.StatusCode.ShouldBe(status);
        outcome.Value.ShouldBeNull();
    }

    [Fact]
    public async Task Propagates_caller_cancellation_to_an_active_retry_summary_request()
    {
        // Arrange
        var testCt = TestContext.Current.CancellationToken;
        var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var response = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var client = ContractHttpClientTestHelper.CreateClient((_, ct) =>
        {
            requestStarted.SetResult();
            return response.Task.WaitAsync(ct);
        });
        var sut = new CatalogGalleryOperationsApiClient(client);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(testCt);

        // Act
        var pending = sut.GetRetryHealth(cancellation.Token);
        await requestStarted.Task.WaitAsync(testCt);
        await cancellation.CancelAsync();

        // Assert
        Func<Task> complete = () => pending.WaitAsync(testCt);
        var exception = await complete.ShouldThrow<OperationCanceledException>();
        exception.CancellationToken.ShouldBe(cancellation.Token);
    }
}
