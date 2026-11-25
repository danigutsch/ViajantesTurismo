using System.Text;
using System.Text.Json;

namespace ViajantesTurismo.Admin.ApiServiceTests.Payments;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, SharedKernel.Testing.TestTraitValues.EndpointCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, SharedKernel.Testing.TestTraitValues.TestServerHost)]
public sealed class PaymentInstructionCommandEndpointTests
{
    [Theory]
    [InlineData("approve", 21, true)]
    [InlineData("approve", 21, false)]
    [InlineData("finalize", 22, true)]
    [InlineData("finalize", 22, false)]
    [InlineData("withdraw-approval", 15, true)]
    [InlineData("withdraw-approval", 15, false)]
    [InlineData("void", 24, true)]
    [InlineData("void", 24, false)]
    [InlineData("review", 0, true)]
    [InlineData("review", 0, false)]
    public async Task Instruction_lifecycle_rejects_missing_snapshots_and_invalid_confirmation(string operation, int reason, bool valid)
    {
        // Arrange
        var sender = new PaymentEndpointSender();
        await using var factory = AdminApiTestHost.Create(services => PaymentEndpointServices.Configure(services, sender, new PaymentAccountQueryProbe()),
            configureConfiguration: configuration => configuration["Payments:Enabled"] = "true");
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.CreateVersion7().ToString("N"));
        var json = operation == "review" ? "{\"expectedDocumentVersion\":2,\"changesRequested\":true}" : JsonSerializer.Serialize(new
        {
            expectedDocumentVersion = 2,
            reason,
            confirmed = valid
        });
        using var input = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        using var response = await client.PostAsync(new Uri("/api/v1/documents/" + Guid.CreateVersion7() + "/payment-instructions/" + operation, UriKind.Relative), input, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(valid || operation == "review" ? HttpStatusCode.NotFound : HttpStatusCode.BadRequest);
        var assertionTarget = response.Headers.CacheControl?.NoStore;
        assertionTarget.ShouldNotBeNull().ShouldBeTrue();
        sender.Calls.ShouldBe(0);
    }

    [Theory]
    [InlineData(false, 400, "rejected")]
    [InlineData(false, 404, "rejected")]
    [InlineData(false, 409, "rejected")]
    [InlineData(false, 503, "failed")]
    [InlineData(true, 400, "rejected")]
    [InlineData(true, 404, "rejected")]
    [InlineData(true, 409, "rejected")]
    [InlineData(true, 503, "failed")]
    public async Task Instruction_generation_preserves_response_and_classifies_telemetry(bool regenerate, int status, string outcome)
    {
        // Arrange
        var sender = new PaymentEndpointSender { Status = status };
        await using var factory = AdminApiTestHost.Create(services => PaymentEndpointServices.Configure(services, sender, new PaymentAccountQueryProbe()),
            configureConfiguration: configuration => configuration["Payments:Enabled"] = "true");
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.CreateVersion7().ToString("N"));
        var path = "/api/v1/documents/bookings/" + Guid.CreateVersion7() + "/payment-instructions" + (regenerate ? "/" + Guid.CreateVersion7() + "/regenerate" : "");
        using var input = new StringContent("""{"expectedVersion":2,"accountId":"11111111-1111-1111-1111-111111111111","expectedAccountVersion":1,"installmentIds":[]}""", Encoding.UTF8, "application/json");
        using var metrics = AdminApiTestHost.RecordMetrics(factory);

        // Act
        using var response = await client.PostAsync(new Uri(path, UriKind.Relative), input, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe((HttpStatusCode)status);
        sender.Calls.ShouldBe(1);
        var assertionTarget = response.Headers.CacheControl?.NoStore;
        assertionTarget.ShouldNotBeNull().ShouldBeTrue();
        var (count, tags) = metrics.Counters("admin.document.operation").ShouldHaveSingleItem();
        count.ShouldBe(1L);
        tags.ShouldBe(
        [
            new KeyValuePair<string, object?>("document.operation", regenerate ? "regenerate" : "generate"),
            new KeyValuePair<string, object?>("document.outcome", outcome),
        ]);
    }

    [Fact]
    public async Task Booking_without_retained_instructions_returns_an_empty_list()
    {
        // Arrange
        await using var factory = AdminApiTestHost.Create(services => PaymentEndpointServices.Configure(services, new PaymentEndpointSender(), new PaymentAccountQueryProbe()),
            configureConfiguration: configuration => configuration["Payments:Enabled"] = "true");
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");

        // Act
        using var response = await client.GetAsync(new Uri("/api/v1/documents/bookings/" + Guid.CreateVersion7() + "/payment-instructions", UriKind.Relative), TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldBe("[]");
        var assertionTarget = response.Headers.CacheControl?.NoStore;
        assertionTarget.ShouldNotBeNull().ShouldBeTrue();
    }

    [Theory]
    [InlineData("Admin", true, "", HttpStatusCode.OK, 25)]
    [InlineData("Admin", true, "?page=99&pageSize=1", HttpStatusCode.OK, 1)]
    [InlineData("Admin", true, "?pageSize=0", HttpStatusCode.BadRequest, 0)]
    [InlineData("Admin", true, "?pageSize=101", HttpStatusCode.BadRequest, 0)]
    [InlineData("Admin", false, "", HttpStatusCode.NotFound, 0)]
    [InlineData("Operator", true, "", HttpStatusCode.Forbidden, 0)]
    public async Task Revision_pages_preserve_administrator_access_and_payment_rollout(string role, bool enabled, string query,
        HttpStatusCode expectedStatus, int expectedPageSize)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(role);
        await using var factory = AdminApiTestHost.Create(
            services => PaymentEndpointServices.Configure(services, new PaymentEndpointSender(), new PaymentAccountQueryProbe()),
            configureConfiguration: configuration => configuration["Payments:Enabled"] = enabled.ToString());
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, role);
        var path = new Uri($"/api/v1/documents/bookings/{Guid.CreateVersion7()}/payment-instructions/page{query}", UriKind.Relative);
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var response = await client.GetAsync(path, ct);

        // Assert
        response.StatusCode.ShouldBe(expectedStatus);
        if (expectedStatus != HttpStatusCode.Forbidden)
        {
            var noStore = response.Headers.CacheControl?.NoStore;
            noStore.ShouldNotBeNull().ShouldBeTrue();
        }
        if (expectedStatus == HttpStatusCode.OK)
        {
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            json.RootElement.GetProperty("items").GetArrayLength().ShouldBe(0);
            json.RootElement.GetProperty("page").GetInt32().ShouldBe(1);
            json.RootElement.GetProperty("pageSize").GetInt32().ShouldBe(expectedPageSize);
            json.RootElement.GetProperty("total").GetInt32().ShouldBe(0);
        }
    }
}
