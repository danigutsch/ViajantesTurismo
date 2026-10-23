using System.Text;

namespace ViajantesTurismo.Admin.ApiServiceTests.Payments;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, PandaBear.Testing.TestTraitValues.EndpointCategory)]
[Trait(PandaBear.Testing.TestTraitNames.HostName, PandaBear.Testing.TestTraitValues.TestServerHost)]
public sealed class PaymentRequestBindingTests
{
    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task Malformed_receipts_return_bad_request_without_executing_financial_commands(string environment)
    {
        // Arrange
        ArgumentException.ThrowIfNullOrWhiteSpace(environment);
        var transaction = new PaymentEndpointTransaction { ResultId = Guid.CreateVersion7() };
        await using var factory = AdminApiTestHost.Create(services =>
        {
            PaymentEndpointServices.Configure(services, transaction, new PaymentAccountQueryProbe());
        }, environment: environment, configureConfiguration: configuration => configuration["Payments:Enabled"] = "true");
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        client.DefaultRequestHeaders.Add(PandaBear.AspNetCore.IdempotencyKeyHeader.Name, Guid.CreateVersion7().ToString("N"));
        using var input = new StringContent("{", Encoding.UTF8, "application/json");

        // Act
        using var response = await client.PostAsync(new Uri($"/api/v1/bookings/{Guid.CreateVersion7()}/finances/receipts", UriKind.Relative),
            input, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        transaction.Calls.ShouldBe(0);
    }
}
