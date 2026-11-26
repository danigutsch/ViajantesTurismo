using System.Text.Json;
using ViajantesTurismo.Admin.Application.Operations;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.ApiServiceTests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, SharedKernel.Testing.TestTraitValues.EndpointCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, SharedKernel.Testing.TestTraitValues.TestServerHost)]
public sealed class MessagingHealthAuthorizationTests
{
    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Reader", HttpStatusCode.Forbidden)]
    [InlineData("Operator", HttpStatusCode.Forbidden)]
    [InlineData("OperationsScheduleController", HttpStatusCode.Forbidden)]
    public async Task Messaging_health_requires_the_admin_role(string? role, HttpStatusCode expected)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var query = new StubMessagingHealthQuery(Task.FromResult(new GetMessagingHealthDto
        {
            ObservedAtUtc = DateTimeOffset.UnixEpoch,
            CatalogTerminalDeliveryFailureCount = 0,
            CatalogOldestTerminalFailureAt = null,
            CatalogRetryCount = 0,
            CatalogOldestRetryReceivedAt = null,
            CatalogDueRetryCount = 0,
        }));
        await using var factory = AdminApiTestHost.Create(services => services.AddSingleton<IMessagingHealthQuery>(query));
        using var client = factory.CreateClient();
        if (role is not null)
        {
            AdminApiTestHost.ConfigureAuthenticatedClient(client, role);
        }

        // Act
        using var response = await client.GetAsync(new Uri("/api/v1/operations/messaging", UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(expected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    public async Task Admin_receives_only_aggregate_delivery_metadata_with_no_store(int count)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var observed = DateTimeOffset.UnixEpoch.AddHours(3);
        var payload = new GetMessagingHealthDto
        {
            ObservedAtUtc = observed,
            AdminPendingOutboxCount = count,
            AdminOldestPendingOutboxEnqueuedAt = count > 0 ? observed.AddHours(-3) : null,
            CatalogTerminalDeliveryFailureCount = count,
            CatalogOldestTerminalFailureAt = count > 0 ? observed.AddHours(-1) : null,
            CatalogRetryCount = 2,
            CatalogOldestRetryReceivedAt = observed.AddHours(-2),
            CatalogDueRetryCount = 1,
        };
        var query = new StubMessagingHealthQuery(Task.FromResult(payload));
        await using var factory = AdminApiTestHost.Create(services => services.AddSingleton<IMessagingHealthQuery>(query));
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");

        // Act
        using var response = await client.GetAsync(new Uri("/api/v1/operations/messaging", UriKind.Relative), ct);
        var document = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        var fields = document.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ToArray();
        fields.ShouldBe([
            "adminOldestPendingOutboxEnqueuedAt", "adminPendingOutboxCount",
            "catalogDueRetryCount", "catalogOldestRetryReceivedAt", "catalogOldestTerminalFailureAt",
            "catalogRetryCount", "catalogTerminalDeliveryFailureCount", "observedAtUtc",
        ]);
        document.GetProperty("adminPendingOutboxCount").GetInt64().ShouldBe(count);
        var oldestOutbox = document.GetProperty("adminOldestPendingOutboxEnqueuedAt");
        DateTimeOffset? oldestOutboxTime = oldestOutbox.ValueKind == JsonValueKind.Null ? null : oldestOutbox.GetDateTimeOffset();
        oldestOutboxTime.ShouldBe(payload.AdminOldestPendingOutboxEnqueuedAt);
        document.GetProperty("catalogTerminalDeliveryFailureCount").GetInt64().ShouldBe(count);
        document.GetProperty("catalogRetryCount").GetInt64().ShouldBe(2);
        document.GetProperty("catalogDueRetryCount").GetInt64().ShouldBe(1);
    }
}
