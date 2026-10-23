using System.Text;
using System.Text.Json;

namespace ViajantesTurismo.Admin.ApiServiceTests.Payments;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, PandaBear.Testing.TestTraitValues.EndpointCategory)]
[Trait(PandaBear.Testing.TestTraitNames.HostName, PandaBear.Testing.TestTraitValues.TestServerHost)]
public sealed class PaymentAccountCommandEndpointTests
{
    [Theory]
    [InlineData("create", 200)]
    [InlineData("create", 400)]
    [InlineData("create", 404)]
    [InlineData("create", 409)]
    [InlineData("update", 200)]
    [InlineData("update", 400)]
    [InlineData("update", 404)]
    [InlineData("update", 409)]
    [InlineData("verify", 200)]
    [InlineData("verify", 400)]
    [InlineData("verify", 404)]
    [InlineData("verify", 409)]
    [InlineData("review", 200)]
    [InlineData("review", 400)]
    [InlineData("review", 404)]
    [InlineData("review", 409)]
    [InlineData("retire", 200)]
    [InlineData("retire", 400)]
    [InlineData("retire", 404)]
    [InlineData("retire", 409)]
    [InlineData("withdraw-verification", 200)]
    [InlineData("withdraw-verification", 400)]
    [InlineData("withdraw-verification", 404)]
    [InlineData("withdraw-verification", 409)]
    public async Task Recipient_commands_preserve_outcomes_and_return_only_masked_projections(string operation, int status)
    {
        // Arrange
        var queries = new PaymentAccountQueryProbe();
        var transaction = new PaymentEndpointTransaction { ResultId = queries.Account.Id, Status = status };
        await using var factory = AdminApiTestHost.Create(services => PaymentEndpointServices.Configure(services, transaction, queries),
            configureConfiguration: configuration => configuration["Payments:Enabled"] = "true");
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.CreateVersion7().ToString("N"));
        var path = "/api/v1/payment-accounts/" + (operation == "create" ? "" : queries.Account.Id + (operation == "update" ? "" : "/" + operation));
        using var request = new HttpRequestMessage(operation == "update" ? HttpMethod.Put : HttpMethod.Post, new Uri(path, UriKind.Relative))
        {
            Content = new StringContent(PaymentEndpointInputs.Account(operation), Encoding.UTF8, "application/json")
        };

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(status == 200 && operation == "create" ? HttpStatusCode.Created : (HttpStatusCode)status);
        var assertionTarget = response.Headers.CacheControl?.NoStore;
        assertionTarget.ShouldNotBeNull().ShouldBeTrue();
        transaction.Calls.ShouldBe(1);
        queries.SensitiveReads.ShouldBe(0);
        body.ShouldNotContain("DE89370400440532013000", StringComparison.Ordinal);
        if (status == 200)
        {
            using var json = JsonDocument.Parse(body);
            json.RootElement.GetProperty("id").GetGuid().ShouldBe(queries.Account.Id);
            json.RootElement.GetProperty("maskedIdentifier").GetString().ShouldBe(queries.Account.MaskedIdentifier);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Account_reads_distinguish_missing_recipients_from_masked_summaries(bool exists)
    {
        // Arrange
        var queries = new PaymentAccountQueryProbe();
        var transaction = new PaymentEndpointTransaction();
        await using var factory = AdminApiTestHost.Create(services => PaymentEndpointServices.Configure(services, transaction, queries),
            configureConfiguration: configuration => configuration["Payments:Enabled"] = "true");
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        var id = exists ? queries.Account.Id : Guid.CreateVersion7();

        // Act
        using var response = await client.GetAsync(new Uri("/api/v1/payment-accounts/" + id, UriKind.Relative), TestContext.Current.CancellationToken);
        using var sensitive = await client.GetAsync(new Uri("/api/v1/payment-accounts/" + id + "/recipient", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(exists ? HttpStatusCode.OK : HttpStatusCode.NotFound);
        sensitive.StatusCode.ShouldBe(exists ? HttpStatusCode.OK : HttpStatusCode.NotFound);
        queries.SensitiveReads.ShouldBe(exists ? 1 : 0);
    }
}
