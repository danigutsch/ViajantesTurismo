using SharedKernel.HttpClients;
using SharedKernel.Testing;
using SharedKernel.Testing.Contracts;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Contracts.Http;

namespace ViajantesTurismo.Admin.ContractTests.ApiClients;

[TestScope(TestScope.Component)]
public sealed class MessagingDeliveryPageApiClientTests
{
    [Theory]
    [InlineData("""{"rows":null,"nextRecordedAt":null,"nextStorageId":null}""")]
    [InlineData("""{"rows":[null],"nextRecordedAt":null,"nextStorageId":null}""")]
    [InlineData("""{"rows":[],"nextRecordedAt":"2026-11-25T10:30:00Z","nextStorageId":null}""")]
    [InlineData("""{"rows":[],"nextRecordedAt":null,"nextStorageId":"11111111-1111-1111-1111-111111111111"}""")]
    [InlineData("""{"rows":[],"nextRecordedAt":"2026-11-25T10:30:00Z","nextStorageId":"00000000-0000-0000-0000-000000000000"}""")]
    public async Task Delivery_pages_reject_null_rows_and_invalid_cursor_pairs(string body)
    {
        // Arrange
        using var client = ContractHttpClientTestHelper.CreateClient(_ => ContractHttpClientTestHelper.JsonResponse(body));
        var sut = new MessagingHealthApiClient(client);
        var ct = TestContext.Current.CancellationToken;

        // Act
        var transport = await sut.ReadCatalogDelivery(new CatalogDeliveryFilters(), ct);
        var outbox = await sut.ReadOutboxDelivery(new OutboxDeliveryFilters(), ct);

        // Assert
        new[] { transport.Kind, outbox.Kind }.ShouldBe([HttpOutcomeKind.MalformedBody, HttpOutcomeKind.MalformedBody]);
        transport.Value.ShouldBeNull();
        outbox.Value.ShouldBeNull();
    }

    [Fact]
    public async Task Delivery_pages_preserve_valid_empty_results_and_a_complete_continuation()
    {
        // Arrange
        const string empty = """{"rows":[],"nextRecordedAt":null,"nextStorageId":null}""";
        const string page = """
            {"rows":[{"storageId":"11111111-1111-1111-1111-111111111111","eventId":"fixture-event","eventType":"TourChanged",
                      "recordedAt":"2026-11-25T10:30:00Z","attempts":1,"lastAttemptAt":null,"nextAttemptAt":null,"terminalAt":null}],
             "nextRecordedAt":"2026-11-25T10:30:00Z","nextStorageId":"11111111-1111-1111-1111-111111111111"}
            """;
        var response = empty;
        using var client = ContractHttpClientTestHelper.CreateClient(_ => ContractHttpClientTestHelper.JsonResponse(response));
        var sut = new MessagingHealthApiClient(client);
        var ct = TestContext.Current.CancellationToken;

        // Act
        var emptyTransport = await sut.ReadCatalogDelivery(new CatalogDeliveryFilters(), ct);
        var emptyOutbox = await sut.ReadOutboxDelivery(new OutboxDeliveryFilters(), ct);
        response = page;
        var populatedTransport = await sut.ReadCatalogDelivery(new CatalogDeliveryFilters(), ct);
        var populatedOutbox = await sut.ReadOutboxDelivery(new OutboxDeliveryFilters(), ct);

        // Assert
        emptyTransport.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        emptyTransport.Value.ShouldNotBeNull().Rows.ShouldBeEmpty();
        emptyOutbox.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        emptyOutbox.Value.ShouldNotBeNull().Rows.ShouldBeEmpty();
        populatedTransport.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        var transportPage = populatedTransport.Value.ShouldNotBeNull();
        var transportRow = transportPage.Rows.ShouldHaveSingleItem();
        transportPage.NextRecordedAt.ShouldBe(transportRow.RecordedAt);
        transportPage.NextStorageId.ShouldBe(transportRow.StorageId);
        populatedOutbox.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        var outboxPage = populatedOutbox.Value.ShouldNotBeNull();
        var outboxRow = outboxPage.Rows.ShouldHaveSingleItem();
        outboxPage.NextRecordedAt.ShouldBe(outboxRow.RecordedAt);
        outboxPage.NextStorageId.ShouldBe(outboxRow.StorageId);
    }

    [Theory]
    [InlineData(false, "11111111-1111-1111-1111-111111111111", "2026-11-25T10:30:00Z", HttpOutcomeKind.MalformedBody)]
    [InlineData(true, "22222222-2222-2222-2222-222222222222", "2026-11-25T10:30:00Z", HttpOutcomeKind.MalformedBody)]
    [InlineData(true, "11111111-1111-1111-1111-111111111111", "2026-11-25T10:29:59Z", HttpOutcomeKind.MalformedBody)]
    [InlineData(true, "11111111-1111-1111-1111-111111111111", "2026-11-25T14:30:00+04:00", HttpOutcomeKind.Succeeded)]
    public async Task A_delivery_continuation_must_identify_the_last_returned_row(
        bool hasRows, string nextStorageId, string nextRecordedAt, HttpOutcomeKind expected)
    {
        // Arrange
        const string rows = """
            [{"storageId":"22222222-2222-2222-2222-222222222222","eventId":"first-event","eventType":"TourChanged",
              "recordedAt":"2026-11-25T10:30:00Z","attempts":0,"lastAttemptAt":null,"nextAttemptAt":null,"terminalAt":null},
             {"storageId":"11111111-1111-1111-1111-111111111111","eventId":"last-event","eventType":"TourChanged",
              "recordedAt":"2026-11-25T10:30:00Z","attempts":0,"lastAttemptAt":null,"nextAttemptAt":null,"terminalAt":null}]
            """;
        var body = $$"""
            {"rows":{{(hasRows ? rows : "[]")}},"nextRecordedAt":"{{nextRecordedAt}}","nextStorageId":"{{nextStorageId}}"}
            """;
        using var client = ContractHttpClientTestHelper.CreateClient(_ => ContractHttpClientTestHelper.JsonResponse(body));
        var sut = new MessagingHealthApiClient(client);
        var ct = TestContext.Current.CancellationToken;

        // Act
        var transport = await sut.ReadCatalogDelivery(new CatalogDeliveryFilters(), ct);
        var outbox = await sut.ReadOutboxDelivery(new OutboxDeliveryFilters(), ct);

        // Assert
        new[] { transport.Kind, outbox.Kind }.ShouldBe([expected, expected]);
        new[] { transport.Value is not null, outbox.Value is not null }
            .ShouldBe([expected == HttpOutcomeKind.Succeeded, expected == HttpOutcomeKind.Succeeded]);
    }

    [Theory]
    [InlineData(true, HttpOutcomeKind.MalformedBody)]
    [InlineData(false, HttpOutcomeKind.Succeeded)]
    public async Task A_delivery_continuation_cannot_repeat_the_submitted_cursor(bool repeatsCursor, HttpOutcomeKind expected)
    {
        // Arrange
        var rows = Enumerable.Range(1, ContractConstants.DefaultCatalogDeliveryPageSize).Reverse()
            .Select(index => FormattableString.Invariant($$"""
                {"storageId":"{{new Guid(index, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0):D}}","eventId":"fixture-{{index}}","eventType":"TourChanged",
                 "recordedAt":"2026-11-25T10:30:00Z","attempts":0,"lastAttemptAt":null,"nextAttemptAt":null,"terminalAt":null}
                """));
        var body = $$"""
            {"rows":[{{string.Join(',', rows)}}],"nextRecordedAt":"2026-11-25T10:30:00Z","nextStorageId":"00000001-0000-0000-0000-000000000000"}
            """;
        var previousPosition = repeatsCursor ? 1 : ContractConstants.DefaultCatalogDeliveryPageSize + 1;
        var previousId = new Guid(previousPosition, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        var previousTime = new DateTimeOffset(2026, 11, 25, 14, 30, 0, TimeSpan.FromHours(4));
        using var client = ContractHttpClientTestHelper.CreateClient(_ => ContractHttpClientTestHelper.JsonResponse(body));
        var sut = new MessagingHealthApiClient(client);
        var ct = TestContext.Current.CancellationToken;

        // Act
        var transport = await sut.ReadCatalogDelivery(
            new CatalogDeliveryFilters(AfterRecordedAt: previousTime, AfterStorageId: previousId), ct);
        var outbox = await sut.ReadOutboxDelivery(
            new OutboxDeliveryFilters(AfterRecordedAt: previousTime, AfterStorageId: previousId), ct);

        // Assert
        new[] { transport.Kind, outbox.Kind }.ShouldBe([expected, expected]);
        new[] { transport.Value is not null, outbox.Value is not null }
            .ShouldBe([expected == HttpOutcomeKind.Succeeded, expected == HttpOutcomeKind.Succeeded]);
    }

    [Theory]
    [InlineData("fixture-event", "TourChanged", "2026-11-25T10:30:00Z", HttpOutcomeKind.Succeeded)]
    [InlineData("Fixture-Event", "TourChanged", "2026-11-25T10:30:00Z", HttpOutcomeKind.MalformedBody)]
    [InlineData("fixture-event", "tourchanged", "2026-11-25T10:30:00Z", HttpOutcomeKind.MalformedBody)]
    [InlineData("fixture-event", "TourChanged", "2026-11-25T10:29:59Z", HttpOutcomeKind.MalformedBody)]
    public async Task Delivery_page_rows_must_match_the_exact_search_and_inclusive_time_bound(
        string eventId, string eventType, string recordedAt, HttpOutcomeKind expected)
    {
        // Arrange
        var body = $$"""
            {"rows":[{"storageId":"11111111-1111-1111-1111-111111111111","eventId":"{{eventId}}","eventType":"{{eventType}}",
                      "recordedAt":"{{recordedAt}}","attempts":0,"lastAttemptAt":null,"nextAttemptAt":null,"terminalAt":null}],
             "nextRecordedAt":null,"nextStorageId":null}
            """;
        var since = new DateTimeOffset(2026, 11, 25, 14, 30, 0, TimeSpan.FromHours(4));
        using var client = ContractHttpClientTestHelper.CreateClient(_ => ContractHttpClientTestHelper.JsonResponse(body));
        var sut = new MessagingHealthApiClient(client);
        var ct = TestContext.Current.CancellationToken;

        // Act
        var transport = await sut.ReadCatalogDelivery(new CatalogDeliveryFilters("fixture-event", "TourChanged", since), ct);
        var outbox = await sut.ReadOutboxDelivery(new OutboxDeliveryFilters("fixture-event", "TourChanged", since), ct);

        // Assert
        new[] { transport.Kind, outbox.Kind }.ShouldBe([expected, expected]);
        new[] { transport.Value is not null, outbox.Value is not null }
            .ShouldBe([expected == HttpOutcomeKind.Succeeded, expected == HttpOutcomeKind.Succeeded]);
    }

    [Theory]
    [InlineData(true, "fixture-event", "true", 0, false, HttpOutcomeKind.Succeeded)]
    [InlineData(true, "fixture-event", "true", 1, false, HttpOutcomeKind.Succeeded)]
    [InlineData(true, "another-event", "true", 1, false, HttpOutcomeKind.MalformedBody)]
    [InlineData(true, "fixture-event", "true", 2, false, HttpOutcomeKind.MalformedBody)]
    [InlineData(true, "fixture-event", "true", 1, true, HttpOutcomeKind.MalformedBody)]
    [InlineData(true, "fixture-event", "null", 1, false, HttpOutcomeKind.MalformedBody)]
    [InlineData(false, "fixture-event", "true", 1, false, HttpOutcomeKind.MalformedBody)]
    public async Task Retained_outbox_results_are_a_single_exact_lookup_in_the_requested_mode(
        bool retained, string eventId, string responseMode, int rowCount, bool hasCursor, HttpOutcomeKind expected)
    {
        // Arrange
        var rows = Enumerable.Range(1, rowCount).Select(index => FormattableString.Invariant($$"""
            {"storageId":"{{new Guid(index, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0):D}}","eventId":"{{eventId}}","eventType":"TourChanged",
             "recordedAt":"2026-11-25T10:30:00Z","attempts":0,"lastAttemptAt":null,"nextAttemptAt":null}
            """));
        var nextAt = hasCursor ? "\"2026-11-25T10:30:00Z\"" : "null";
        var nextId = hasCursor ? "\"00000001-0000-0000-0000-000000000000\"" : "null";
        var body = $$"""
            {"rows":[{{string.Join(',', rows)}}],"nextRecordedAt":{{nextAt}},"nextStorageId":{{nextId}},"includesPublished":{{responseMode}}}
            """;
        using var client = ContractHttpClientTestHelper.CreateClient(_ => ContractHttpClientTestHelper.JsonResponse(body));
        var sut = new MessagingHealthApiClient(client);
        var filters = new OutboxDeliveryFilters("fixture-event") { IncludePublished = retained };

        // Act
        var outcome = await sut.ReadOutboxDelivery(filters, TestContext.Current.CancellationToken);
        var hasValue = outcome.Value is not null;

        // Assert
        outcome.Kind.ShouldBe(expected);
        hasValue.ShouldBe(expected == HttpOutcomeKind.Succeeded);
    }

    [Fact]
    public async Task Pending_messaging_pages_cannot_exceed_the_requested_bound()
    {
        // Arrange
        var rows = Enumerable.Range(1, Math.Max(ContractConstants.DefaultOutboxDeliveryPageSize, ContractConstants.DefaultCatalogDeliveryPageSize) + 1)
            .Select(index => FormattableString.Invariant($$"""
                {"storageId":"{{new Guid(index, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0):D}}","eventId":"fixture-{{index}}","eventType":"TourChanged",
                 "recordedAt":"2026-11-25T10:30:00Z","attempts":0,"lastAttemptAt":null,"nextAttemptAt":null,"terminalAt":null}
                """));
        var body = $$"""{"rows":[{{string.Join(',', rows)}}],"nextRecordedAt":null,"nextStorageId":null}""";
        using var client = ContractHttpClientTestHelper.CreateClient(_ => ContractHttpClientTestHelper.JsonResponse(body));
        var sut = new MessagingHealthApiClient(client);
        var ct = TestContext.Current.CancellationToken;

        // Act
        var transport = await sut.ReadCatalogDelivery(new CatalogDeliveryFilters(), ct);
        var outbox = await sut.ReadOutboxDelivery(new OutboxDeliveryFilters(), ct);

        // Assert
        new[] { transport.Kind, outbox.Kind }.ShouldBe([HttpOutcomeKind.MalformedBody, HttpOutcomeKind.MalformedBody]);
        transport.Value.ShouldBeNull();
        outbox.Value.ShouldBeNull();
    }
}
