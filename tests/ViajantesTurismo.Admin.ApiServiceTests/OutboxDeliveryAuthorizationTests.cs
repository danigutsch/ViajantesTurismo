using System.Text.Json;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Application.Operations;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.ApiServiceTests;

[Trait(TestTraitNames.CategoryName, TestTraitValues.EndpointCategory)]
[Trait(TestTraitNames.HostName, TestTraitValues.TestServerHost)]
public sealed class OutboxDeliveryAuthorizationTests
{
    private const string Route = "/api/v1/operations/messaging/outbox";

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Reader", HttpStatusCode.Forbidden)]
    [InlineData("Operator", HttpStatusCode.Forbidden)]
    [InlineData("OperationsScheduleController", HttpStatusCode.Forbidden)]
    public async Task Pending_outbox_page_requires_the_admin_role(string? role, HttpStatusCode expected)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var query = new StubOutboxDeliveryQuery(new GetOutboxDeliveryPageDto
        {
            Rows = [],
            NextRecordedAt = null,
            NextStorageId = null,
        });
        await using var factory = AdminApiTestHost.Create(services => services.AddSingleton<IOutboxDeliveryQuery>(query));
        using var client = factory.CreateClient();
        if (role is not null)
        {
            AdminApiTestHost.ConfigureAuthenticatedClient(client, role);
        }

        // Act
        using var response = await client.GetAsync(new Uri(Route, UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(expected);
        query.Request.ShouldBeNull();
    }

    [Theory]
    [InlineData("limit=0")]
    [InlineData("limit=101")]
    [InlineData("afterRecordedAt=2026-01-01T00:00:00Z")]
    [InlineData("afterStorageId=00000000-0000-0000-0000-000000000001")]
    [InlineData("includePublished=true")]
    [InlineData("includePublished=true&eventId=%20")]
    [InlineData("includePublished=true&eventId=envelope-1&afterRecordedAt=2026-01-01T00:00:00Z&afterStorageId=00000000-0000-0000-0000-000000000001")]
    public async Task Invalid_page_bounds_and_incomplete_cursors_never_reach_the_query(string queryString)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var query = new StubOutboxDeliveryQuery(new GetOutboxDeliveryPageDto
        {
            Rows = [],
            NextRecordedAt = null,
            NextStorageId = null,
        });
        await using var factory = AdminApiTestHost.Create(services => services.AddSingleton<IOutboxDeliveryQuery>(query));
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");

        // Act
        using var response = await client.GetAsync(new Uri($"{Route}?{queryString}", UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        query.Request.ShouldBeNull();
    }

    [Fact]
    public async Task Admin_receives_only_pending_publication_metadata_and_the_continuation()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var recordedAt = DateTimeOffset.UnixEpoch.AddHours(1);
        var id = Guid.CreateVersion7();
        var query = new StubOutboxDeliveryQuery(new GetOutboxDeliveryPageDto
        {
            Rows =
            [
                new OutboxDeliveryRowDto
                {
                    StorageId = id,
                    EventId = "envelope-1",
                    EventType = "catalog.test",
                    RecordedAt = recordedAt,
                    Attempts = 2,
                    LastAttemptAt = recordedAt.AddMinutes(1),
                    NextAttemptAt = null,
                },
            ],
            NextRecordedAt = recordedAt,
            NextStorageId = id,
        });
        await using var factory = AdminApiTestHost.Create(services => services.AddSingleton<IOutboxDeliveryQuery>(query));
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");

        // Act
        using var response = await client.GetAsync(new Uri($"{Route}?eventId=envelope-1&eventType=catalog.test&limit=1", UriKind.Relative), ct);
        var document = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        query.Request.ShouldBe(new OutboxDeliveryRequest("envelope-1", "catalog.test", null, null, null, 1));
        document.GetProperty("nextStorageId").GetGuid().ShouldBe(id);
        var fields = document.GetProperty("rows")[0].EnumerateObject()
            .Select(property => property.Name).Order(StringComparer.Ordinal).ToArray();
        fields.ShouldBe(["attempts", "eventId", "eventType", "lastAttemptAt", "nextAttemptAt", "recordedAt", "storageId"]);
    }
}
