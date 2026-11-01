using System.Text;
using System.Text.Json;

namespace ViajantesTurismo.Admin.ApiServiceTests.Payments;

/// <summary>Guards required payment operation keys before application workflows inspect state.</summary>
[Trait(PandaBear.Testing.TestTraitNames.HostName, PandaBear.Testing.TestTraitValues.TestServerHost)]
public sealed class PaymentIdempotencyEndpointTests
{
    private const string OversizedKey = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Theory]
    [InlineData("approve", null)]
    [InlineData("finalize", null)]
    [InlineData("withdraw-approval", null)]
    [InlineData("void", null)]
    [InlineData("review", null)]
    [InlineData("approve", "invalid key")]
    [InlineData("approve", " ")]
    [InlineData("approve", "first,second")]
    [InlineData("approve", "key/value")]
    [InlineData("approve", "key?value")]
    [InlineData("approve", "café")]
    [InlineData("approve", OversizedKey)]
    public async Task Invalid_keys_are_rejected_before_instruction_state_is_loaded(string operation, string? key)
    {
        // Arrange
        var transaction = new PaymentEndpointTransaction();
        await using var factory = AdminApiTestHost.Create(services => PaymentEndpointServices.Configure(services, transaction, new PaymentAccountQueryProbe()),
            configureConfiguration: configuration => configuration["Payments:Enabled"] = "true");
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/api/v1/documents/" + Guid.CreateVersion7() + "/payment-instructions/" + operation, UriKind.Relative))
        {
            Content = new StringContent("{\"expectedDocumentVersion\":2,\"changesRequested\":true,\"confirmed\":true,\"reason\":21}", Encoding.UTF8, "application/json")
        };
        if (key is not null)
        {
            request.Headers.TryAddWithoutValidation("Idempotency-Key", key).ShouldBeTrue();
        }

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        body.RootElement.GetProperty("errors").TryGetProperty("Idempotency-Key", out _).ShouldBeTrue();
        transaction.Calls.ShouldBe(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Valid_padded_keys_reach_the_instruction_workflow(bool maximumLength)
    {
        // Arrange
        await using var factory = AdminApiTestHost.Create(services => PaymentEndpointServices.Configure(services, new PaymentEndpointTransaction(), new PaymentAccountQueryProbe()),
            configureConfiguration: configuration => configuration["Payments:Enabled"] = "true");
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        var key = maximumLength ? new string('a', 255) : "valid-key";
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/api/v1/documents/" + Guid.CreateVersion7() + "/payment-instructions/approve", UriKind.Relative))
        {
            Content = new StringContent("{\"expectedDocumentVersion\":2,\"confirmed\":true,\"reason\":21}", Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("Idempotency-Key", "  " + key + "  ").ShouldBeTrue();

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
