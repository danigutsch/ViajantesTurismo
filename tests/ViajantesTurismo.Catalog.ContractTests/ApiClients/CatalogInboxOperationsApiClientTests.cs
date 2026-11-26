using SharedKernel.HttpClients;
using SharedKernel.Testing;
using SharedKernel.Testing.Contracts;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Contracts.Http;

namespace ViajantesTurismo.Catalog.ContractTests.ApiClients;

[Trait(TestTraitNames.CategoryName, TestTraitValues.ContractCategory)]
[TestScope(TestScope.Component)]
public sealed class CatalogInboxOperationsApiClientTests
{
    [Theory]
    [InlineData("{}")]
    [InlineData("""{"duplicateCount":0,"lastObservedAt":null}""")]
    [InlineData("""{"entriesWithDuplicates":0,"lastObservedAt":null}""")]
    [InlineData("""{"entriesWithDuplicates":0,"duplicateCount":0}""")]
    public async Task ReadHealth_rejects_a_success_body_missing_required_observations(string body)
    {
        // Arrange
        using var client = ContractHttpClientTestHelper.CreateClient(_ => ContractHttpClientTestHelper.JsonResponse(body));
        var sut = new CatalogInboxOperationsApiClient(client);

        // Act
        var outcome = await sut.ReadHealth(TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.MalformedBody);
        outcome.Value.ShouldBeNull();
    }

    [Theory]
    [InlineData(0, 0, "null")]
    [InlineData(2, 5, "\"2026-11-25T10:30:00Z\"")]
    public async Task ReadHealth_preserves_reported_duplicate_counts(long entries, long duplicates, string observation)
    {
        // Arrange
        var body = FormattableString.Invariant($$"""
            {"entriesWithDuplicates":{{entries}},"duplicateCount":{{duplicates}},"lastObservedAt":{{observation}},"consumerName":"Catalog"}
            """);
        using var client = ContractHttpClientTestHelper.CreateClient(_ => ContractHttpClientTestHelper.JsonResponse(body));
        var sut = new CatalogInboxOperationsApiClient(client);

        // Act
        var outcome = await sut.ReadHealth(TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        var health = outcome.Value.ShouldNotBeNull();
        health.EntriesWithDuplicates.ShouldBe(entries);
        health.DuplicateCount.ShouldBe(duplicates);
        health.ConsumerName.ShouldBe("Catalog");
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"rows":null,"nextObservedAt":null,"nextEventId":null}""")]
    [InlineData("""{"rows":[null],"nextObservedAt":null,"nextEventId":null}""")]
    [InlineData("""{"rows":[],"nextEventId":null}""")]
    [InlineData("""{"rows":[],"nextObservedAt":"2026-11-25T10:30:00Z","nextEventId":null}""")]
    [InlineData("""{"rows":[],"nextObservedAt":null,"nextEventId":"11111111-1111-1111-1111-111111111111"}""")]
    [InlineData("""{"rows":[],"nextObservedAt":"2026-11-25T10:30:00Z","nextEventId":"00000000-0000-0000-0000-000000000000"}""")]
    public async Task ReadPage_rejects_missing_rows_null_entries_and_invalid_cursor_pairs(string body)
    {
        // Arrange
        using var client = ContractHttpClientTestHelper.CreateClient(_ => ContractHttpClientTestHelper.JsonResponse(body));
        var sut = new CatalogInboxOperationsApiClient(client);

        // Act
        var outcome = await sut.ReadPage(new CatalogInboxDuplicateFilters(), TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.MalformedBody);
        outcome.Value.ShouldBeNull();
    }

    [Fact]
    public async Task ReadPage_preserves_valid_empty_results_and_a_complete_continuation()
    {
        // Arrange
        const string empty = """{"rows":[],"nextObservedAt":null,"nextEventId":null}""";
        const string page = """
            {"rows":[{"eventId":"11111111-1111-1111-1111-111111111111","duplicateCount":2,"lastObservedAt":"2026-11-25T10:30:00Z"}],
             "nextObservedAt":"2026-11-25T10:30:00Z","nextEventId":"11111111-1111-1111-1111-111111111111"}
            """;
        var response = empty;
        using var client = ContractHttpClientTestHelper.CreateClient(_ => ContractHttpClientTestHelper.JsonResponse(response));
        var sut = new CatalogInboxOperationsApiClient(client);
        var ct = TestContext.Current.CancellationToken;

        // Act
        var emptyResult = await sut.ReadPage(new CatalogInboxDuplicateFilters(), ct);
        response = page;
        var populatedResult = await sut.ReadPage(new CatalogInboxDuplicateFilters(), ct);

        // Assert
        emptyResult.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        emptyResult.Value.ShouldNotBeNull().Rows.ShouldBeEmpty();
        populatedResult.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        var actualPage = populatedResult.Value.ShouldNotBeNull();
        var row = actualPage.Rows.ShouldHaveSingleItem();
        actualPage.NextObservedAt.ShouldBe(row.LastObservedAt);
        actualPage.NextEventId.ShouldBe(row.EventId);
    }

    [Theory]
    [InlineData(false, "11111111-1111-1111-1111-111111111111", "2026-11-25T10:30:00Z", HttpOutcomeKind.MalformedBody)]
    [InlineData(true, "22222222-2222-2222-2222-222222222222", "2026-11-25T10:30:00Z", HttpOutcomeKind.MalformedBody)]
    [InlineData(true, "11111111-1111-1111-1111-111111111111", "2026-11-25T10:29:59Z", HttpOutcomeKind.MalformedBody)]
    [InlineData(true, "11111111-1111-1111-1111-111111111111", "2026-11-25T14:30:00+04:00", HttpOutcomeKind.Succeeded)]
    public async Task An_inbox_continuation_must_identify_the_last_returned_row(
        bool hasRows, string nextEventId, string nextObservedAt, HttpOutcomeKind expected)
    {
        // Arrange
        const string rows = """
            [{"eventId":"22222222-2222-2222-2222-222222222222","duplicateCount":1,"lastObservedAt":"2026-11-25T10:30:00Z"},
             {"eventId":"11111111-1111-1111-1111-111111111111","duplicateCount":2,"lastObservedAt":"2026-11-25T10:30:00Z"}]
            """;
        var body = $$"""
            {"rows":{{(hasRows ? rows : "[]")}},"nextObservedAt":"{{nextObservedAt}}","nextEventId":"{{nextEventId}}"}
            """;
        using var client = ContractHttpClientTestHelper.CreateClient(_ => ContractHttpClientTestHelper.JsonResponse(body));
        var sut = new CatalogInboxOperationsApiClient(client);

        // Act
        var outcome = await sut.ReadPage(new CatalogInboxDuplicateFilters(), TestContext.Current.CancellationToken);
        var hasValue = outcome.Value is not null;

        // Assert
        outcome.Kind.ShouldBe(expected);
        hasValue.ShouldBe(expected == HttpOutcomeKind.Succeeded);
    }

    [Theory]
    [InlineData("11111111-1111-1111-1111-111111111111", HttpOutcomeKind.MalformedBody)]
    [InlineData("22222222-2222-2222-2222-222222222222", HttpOutcomeKind.Succeeded)]
    public async Task An_inbox_continuation_cannot_repeat_the_submitted_cursor(string previousEventId, HttpOutcomeKind expected)
    {
        // Arrange
        const string body = """
            {"rows":[{"eventId":"11111111-1111-1111-1111-111111111111","duplicateCount":2,"lastObservedAt":"2026-11-25T10:30:00Z"}],
             "nextObservedAt":"2026-11-25T10:30:00Z","nextEventId":"11111111-1111-1111-1111-111111111111"}
            """;
        var previousTime = new DateTimeOffset(2026, 11, 25, 14, 30, 0, TimeSpan.FromHours(4));
        var filters = new CatalogInboxDuplicateFilters(AfterObservedAt: previousTime, AfterEventId: Guid.Parse(previousEventId), Limit: 1);
        using var client = ContractHttpClientTestHelper.CreateClient(_ => ContractHttpClientTestHelper.JsonResponse(body));
        var sut = new CatalogInboxOperationsApiClient(client);

        // Act
        var outcome = await sut.ReadPage(filters, TestContext.Current.CancellationToken);
        var hasValue = outcome.Value is not null;

        // Assert
        outcome.Kind.ShouldBe(expected);
        hasValue.ShouldBe(expected == HttpOutcomeKind.Succeeded);
    }

    [Theory]
    [InlineData("11111111-1111-1111-1111-111111111111", "2026-11-25T10:30:00Z", HttpOutcomeKind.Succeeded)]
    [InlineData("22222222-2222-2222-2222-222222222222", "2026-11-25T10:30:00Z", HttpOutcomeKind.MalformedBody)]
    [InlineData("11111111-1111-1111-1111-111111111111", "2026-11-25T10:29:59Z", HttpOutcomeKind.MalformedBody)]
    public async Task Duplicate_rows_must_match_the_exact_event_and_inclusive_observation_bound(
        string eventId, string observedAt, HttpOutcomeKind expected)
    {
        // Arrange
        var body = $$"""
            {"rows":[{"eventId":"{{eventId}}","duplicateCount":2,"lastObservedAt":"{{observedAt}}"}],
             "nextObservedAt":null,"nextEventId":null}
            """;
        var filters = new CatalogInboxDuplicateFilters(EventId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ObservedSince: new DateTimeOffset(2026, 11, 25, 14, 30, 0, TimeSpan.FromHours(4)));
        using var client = ContractHttpClientTestHelper.CreateClient(_ => ContractHttpClientTestHelper.JsonResponse(body));
        var sut = new CatalogInboxOperationsApiClient(client);

        // Act
        var outcome = await sut.ReadPage(filters, TestContext.Current.CancellationToken);
        var hasValue = outcome.Value is not null;

        // Assert
        outcome.Kind.ShouldBe(expected);
        hasValue.ShouldBe(expected == HttpOutcomeKind.Succeeded);
    }

    [Fact]
    public async Task Duplicate_pages_cannot_exceed_the_requested_bound()
    {
        // Arrange
        const string body = """
            {"rows":[{"eventId":"22222222-2222-2222-2222-222222222222","duplicateCount":2,"lastObservedAt":"2026-11-25T10:30:00Z"},
                     {"eventId":"11111111-1111-1111-1111-111111111111","duplicateCount":1,"lastObservedAt":"2026-11-25T10:29:00Z"}],
             "nextObservedAt":null,"nextEventId":null}
            """;
        using var client = ContractHttpClientTestHelper.CreateClient(_ => ContractHttpClientTestHelper.JsonResponse(body));
        var sut = new CatalogInboxOperationsApiClient(client);

        // Act
        var outcome = await sut.ReadPage(new CatalogInboxDuplicateFilters(Limit: 1), TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.MalformedBody);
        outcome.Value.ShouldBeNull();
    }
}
