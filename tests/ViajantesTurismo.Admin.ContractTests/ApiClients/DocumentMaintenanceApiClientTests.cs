using System.Net;
using SharedKernel.HttpClients;
using SharedKernel.Testing;
using SharedKernel.Testing.Contracts;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Contracts.Http;

namespace ViajantesTurismo.Admin.ContractTests.ApiClients;

[Trait(TestTraitNames.CategoryName, TestTraitValues.ContractCategory)]
public sealed class DocumentMaintenanceApiClientTests
{
    [Theory]
    [InlineData("{}")]
    [InlineData("""{"draftRuns":[],"auditRuns":[],"idempotencyRuns":[],"schedules":null}""")]
    [InlineData("""{"draftRuns":null,"auditRuns":[],"idempotencyRuns":[],"schedules":[]}""")]
    [InlineData("""{"draftRuns":[],"auditRuns":[],"idempotencyRuns":[],"schedules":[null]}""")]
    public async Task History_rejects_missing_or_null_collections_and_entries_in_a_success_response(string payload)
    {
        // Arrange
        using var http = ContractHttpClientTestHelper.CreateClient(_ => ContractHttpClientTestHelper.JsonResponse(payload, HttpStatusCode.OK));
        var client = new DocumentMaintenanceApiClient(http);

        // Act
        var outcome = await client.Read(TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.MalformedBody);
        outcome.StatusCode.ShouldBe(HttpStatusCode.OK);
        outcome.Value.ShouldBeNull();
    }

    [Theory]
    [InlineData("""{"runs":null}""")]
    [InlineData("""{"runs":[null]}""")]
    public async Task Batch_timeline_rejects_null_runs_in_a_success_response(string payload)
    {
        // Arrange
        using var http = ContractHttpClientTestHelper.CreateClient(_ => ContractHttpClientTestHelper.JsonResponse(payload, HttpStatusCode.OK));
        var client = new DocumentMaintenanceApiClient(http);

        // Act
        var outcome = await client.Read(DocumentMaintenanceJob.Drafts, before: null, TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.MalformedBody);
        outcome.StatusCode.ShouldBe(HttpStatusCode.OK);
        outcome.Value.ShouldBeNull();
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"executions":null,"next":null}""")]
    [InlineData("""{"executions":[null],"next":null}""")]
    public async Task Executions_rejects_missing_or_null_collections_and_entries_in_a_success_response(string payload)
    {
        // Arrange
        using var http = ContractHttpClientTestHelper.CreateClient(_ => ContractHttpClientTestHelper.JsonResponse(payload, HttpStatusCode.OK));
        var client = new DocumentMaintenanceApiClient(http);

        // Act
        var outcome = await client.ReadExecutions(DocumentMaintenanceJob.Drafts, before: null, TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.MalformedBody);
        outcome.StatusCode.ShouldBe(HttpStatusCode.OK);
        outcome.Value.ShouldBeNull();
    }

    [Fact]
    public async Task History_accepts_empty_batches_and_an_unregistered_schedule_with_optional_values_absent()
    {
        // Arrange
        const string payload = """
            {"draftRuns":[],"auditRuns":[],"idempotencyRuns":[],"schedules":[{"job":"Drafts","isPaused":null}]}
            """;
        using var http = ContractHttpClientTestHelper.CreateClient(_ => ContractHttpClientTestHelper.JsonResponse(payload, HttpStatusCode.OK));
        var client = new DocumentMaintenanceApiClient(http);

        // Act
        var outcome = await client.Read(TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        var history = outcome.Value.ShouldNotBeNull();
        history.DraftRuns.ShouldBeEmpty();
        history.AuditRuns.ShouldBeEmpty();
        history.IdempotencyRuns.ShouldBeEmpty();
        history.Schedules.ShouldHaveSingleItem().IsPaused.ShouldBeNull();
    }
}
