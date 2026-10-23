using System.Text;
using System.Text.Json;

namespace ViajantesTurismo.Admin.ApiServiceTests.Payments;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, PandaBear.Testing.TestTraitValues.EndpointCategory)]
[Trait(PandaBear.Testing.TestTraitNames.HostName, PandaBear.Testing.TestTraitValues.TestServerHost)]
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
        var transaction = new PaymentEndpointTransaction();
        await using var factory = AdminApiTestHost.Create(services => PaymentEndpointServices.Configure(services, transaction, new PaymentAccountQueryProbe()),
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
        transaction.Calls.ShouldBe(0);
    }

    [Theory]
    [InlineData(false, 400)]
    [InlineData(false, 404)]
    [InlineData(false, 409)]
    [InlineData(true, 400)]
    [InlineData(true, 404)]
    [InlineData(true, 409)]
    public async Task Instruction_generation_preserves_rejected_source_and_revision_outcomes(bool regenerate, int status)
    {
        // Arrange
        var transaction = new PaymentEndpointTransaction { Status = status };
        await using var factory = AdminApiTestHost.Create(services => PaymentEndpointServices.Configure(services, transaction, new PaymentAccountQueryProbe()),
            configureConfiguration: configuration => configuration["Payments:Enabled"] = "true");
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.CreateVersion7().ToString("N"));
        var path = "/api/v1/documents/bookings/" + Guid.CreateVersion7() + "/payment-instructions" + (regenerate ? "/" + Guid.CreateVersion7() + "/regenerate" : "");
        using var input = new StringContent("""{"expectedVersion":2,"accountId":"11111111-1111-1111-1111-111111111111","expectedAccountVersion":1,"installmentIds":[]}""", Encoding.UTF8, "application/json");

        // Act
        using var response = await client.PostAsync(new Uri(path, UriKind.Relative), input, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe((HttpStatusCode)status);
        transaction.Calls.ShouldBe(1);
        var assertionTarget = response.Headers.CacheControl?.NoStore;
        assertionTarget.ShouldNotBeNull().ShouldBeTrue();
    }

    [Fact]
    public async Task Booking_without_retained_instructions_returns_an_empty_list()
    {
        // Arrange
        await using var factory = AdminApiTestHost.Create(services => PaymentEndpointServices.Configure(services, new PaymentEndpointTransaction(), new PaymentAccountQueryProbe()),
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
}
