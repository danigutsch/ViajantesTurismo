using System.Text;
using System.Text.Json;

namespace ViajantesTurismo.Admin.ApiServiceTests.Payments;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, PandaBear.Testing.TestTraitValues.EndpointCategory)]
[Trait(PandaBear.Testing.TestTraitNames.HostName, PandaBear.Testing.TestTraitValues.TestServerHost)]
public sealed class BookingFinancialCommandEndpointTests
{
    [Theory]
    [InlineData("schedules", 200)]
    [InlineData("schedules", 400)]
    [InlineData("schedules", 404)]
    [InlineData("schedules", 409)]
    [InlineData("schedules/{id}/approve", 200)]
    [InlineData("schedules/{id}/approve", 400)]
    [InlineData("schedules/{id}/approve", 404)]
    [InlineData("schedules/{id}/approve", 409)]
    [InlineData("quotes", 200)]
    [InlineData("quotes", 400)]
    [InlineData("quotes", 404)]
    [InlineData("quotes", 409)]
    [InlineData("quotes/{id}/approve", 200)]
    [InlineData("quotes/{id}/approve", 400)]
    [InlineData("quotes/{id}/approve", 404)]
    [InlineData("quotes/{id}/approve", 409)]
    [InlineData("receipts", 200)]
    [InlineData("receipts", 400)]
    [InlineData("receipts", 404)]
    [InlineData("receipts", 409)]
    [InlineData("receipts/{id}/reconciliations", 200)]
    [InlineData("receipts/{id}/reconciliations", 400)]
    [InlineData("receipts/{id}/reconciliations", 404)]
    [InlineData("receipts/{id}/reconciliations", 409)]
    [InlineData("reconciliations/{id}/approve", 200)]
    [InlineData("reconciliations/{id}/approve", 400)]
    [InlineData("reconciliations/{id}/approve", 404)]
    [InlineData("reconciliations/{id}/approve", 409)]
    [InlineData("receipts/{id}/allocations", 200)]
    [InlineData("receipts/{id}/allocations", 400)]
    [InlineData("receipts/{id}/allocations", 404)]
    [InlineData("receipts/{id}/allocations", 409)]
    [InlineData("allocation-releases", 200)]
    [InlineData("allocation-releases", 400)]
    [InlineData("allocation-releases", 404)]
    [InlineData("allocation-releases", 409)]
    [InlineData("receipts/{id}/reversals", 200)]
    [InlineData("receipts/{id}/reversals", 400)]
    [InlineData("receipts/{id}/reversals", 404)]
    [InlineData("receipts/{id}/reversals", 409)]
    [InlineData("receipts/{id}/refunds", 200)]
    [InlineData("receipts/{id}/refunds", 400)]
    [InlineData("receipts/{id}/refunds", 404)]
    [InlineData("receipts/{id}/refunds", 409)]
    [InlineData("refunds/{id}/approve", 200)]
    [InlineData("refunds/{id}/approve", 400)]
    [InlineData("refunds/{id}/approve", 404)]
    [InlineData("refunds/{id}/approve", 409)]
    [InlineData("refunds/{id}/corrections", 200)]
    [InlineData("refunds/{id}/corrections", 400)]
    [InlineData("refunds/{id}/corrections", 404)]
    [InlineData("refunds/{id}/corrections", 409)]
    [InlineData("records/{id}/review", 200)]
    [InlineData("records/{id}/review", 400)]
    [InlineData("records/{id}/review", 404)]
    [InlineData("records/{id}/review", 409)]
    [InlineData("records/{id}/withdraw-approval", 200)]
    [InlineData("records/{id}/withdraw-approval", 400)]
    [InlineData("records/{id}/withdraw-approval", 404)]
    [InlineData("records/{id}/withdraw-approval", 409)]
    public async Task Financial_commands_preserve_service_outcomes_and_require_idempotency(string route, int status)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(route);
        var bookingId = Guid.CreateVersion7();
        var transaction = new PaymentEndpointTransaction { ResultId = Guid.CreateVersion7(), Status = status };
        await using var factory = AdminApiTestHost.Create(services =>
        {
            PaymentEndpointServices.Configure(services, transaction, new PaymentAccountQueryProbe());
        }, configureConfiguration: configuration => { configuration["Payments:Enabled"] = "true"; configuration["Payments:BusinessTimeZone"] = "UTC"; });
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        var path = "/api/v1/bookings/" + bookingId + "/finances/" + route.Replace("{id}", transaction.ResultId.ToString(), StringComparison.Ordinal);
        var payload = PaymentEndpointInputs.ForRoute(route);
        using var first = new StringContent(payload, Encoding.UTF8, "application/json");

        // Act
        using var missingKey = await client.PostAsync(new Uri(path, UriKind.Relative), first, TestContext.Current.CancellationToken);
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.CreateVersion7().ToString("N"));
        using var input = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(new Uri(path, UriKind.Relative), input, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        missingKey.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.StatusCode.ShouldBe((HttpStatusCode)status);
        var assertionTarget = response.Headers.CacheControl?.NoStore;
        assertionTarget.ShouldNotBeNull().ShouldBeTrue();
        transaction.Calls.ShouldBe(1);
        if (status == 200)
        {
            using var json = JsonDocument.Parse(body);
            json.RootElement.GetProperty("resourceId").GetGuid().ShouldBe(transaction.ResultId);
        }
        else
        {
            using var problem = JsonDocument.Parse(body);
            problem.RootElement.GetProperty("status").GetInt32().ShouldBe(status);
        }
    }
}
