namespace ViajantesTurismo.Admin.ApiServiceTests;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, PandaBear.Testing.TestTraitValues.EndpointCategory)]
[Trait(PandaBear.Testing.TestTraitNames.HostName, PandaBear.Testing.TestTraitValues.TestServerHost)]
public sealed class PaymentAuthorizationTests
{
    [Theory]
    [InlineData("GET", "/api/v1/payment-accounts/11111111-1111-1111-1111-111111111111/recipient")]
    [InlineData("POST", "/api/v1/payment-accounts/")]
    [InlineData("PUT", "/api/v1/payment-accounts/11111111-1111-1111-1111-111111111111")]
    [InlineData("POST", "/api/v1/payment-accounts/11111111-1111-1111-1111-111111111111/verify")]
    [InlineData("POST", "/api/v1/payment-accounts/11111111-1111-1111-1111-111111111111/retire")]
    [InlineData("POST", "/api/v1/payment-accounts/11111111-1111-1111-1111-111111111111/review")]
    [InlineData("GET", "/api/v1/bookings/11111111-1111-1111-1111-111111111111/finances/details")]
    [InlineData("POST", "/api/v1/bookings/11111111-1111-1111-1111-111111111111/finances/schedules")]
    [InlineData("POST", "/api/v1/bookings/11111111-1111-1111-1111-111111111111/finances/quotes")]
    [InlineData("POST", "/api/v1/bookings/11111111-1111-1111-1111-111111111111/finances/schedules/22222222-2222-2222-2222-222222222222/approve")]
    [InlineData("POST", "/api/v1/bookings/11111111-1111-1111-1111-111111111111/finances/reconciliations/22222222-2222-2222-2222-222222222222/approve")]
    [InlineData("POST", "/api/v1/bookings/11111111-1111-1111-1111-111111111111/finances/receipts/22222222-2222-2222-2222-222222222222/refunds")]
    [InlineData("POST", "/api/v1/bookings/11111111-1111-1111-1111-111111111111/finances/receipts/22222222-2222-2222-2222-222222222222/reversals")]
    [InlineData("POST", "/api/v1/bookings/11111111-1111-1111-1111-111111111111/finances/refunds/22222222-2222-2222-2222-222222222222/approve")]
    [InlineData("POST", "/api/v1/documents/bookings/11111111-1111-1111-1111-111111111111/payment-instructions")]
    [InlineData("POST", "/api/v1/documents/11111111-1111-1111-1111-111111111111/payment-instructions/finalize")]
    [InlineData("POST", "/api/v1/documents/11111111-1111-1111-1111-111111111111/payment-instructions/approve")]
    public async Task Operators_cannot_access_sensitive_accounts_or_approve_financial_actions(string method, string path)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(path);
        await using var factory = AdminApiTestHost.Create();
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Operator");
        using var request = AdminApiMutationRequestFactory.Create(method, path);

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("GET", "/api/v1/payment-accounts/")]
    [InlineData("POST", "/api/v1/bookings/11111111-1111-1111-1111-111111111111/finances/receipts")]
    [InlineData("POST", "/api/v1/payment-accounts/")]
    public async Task Payment_routes_reject_anonymous_callers(string method, string path)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(path);
        await using var factory = AdminApiTestHost.Create();
        using var client = factory.CreateClient();
        using var request = AdminApiMutationRequestFactory.Create(method, path);

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
