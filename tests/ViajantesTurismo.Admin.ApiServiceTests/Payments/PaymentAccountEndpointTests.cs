using Microsoft.Extensions.DependencyInjection.Extensions;
using ViajantesTurismo.Admin.Application.Payments;

namespace ViajantesTurismo.Admin.ApiServiceTests.Payments;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, PandaBear.Testing.TestTraitValues.EndpointCategory)]
[Trait(PandaBear.Testing.TestTraitNames.HostName, PandaBear.Testing.TestTraitValues.TestServerHost)]
public sealed class PaymentAccountEndpointTests
{
    [Theory]
    [InlineData(true, "Admin", HttpStatusCode.OK)]
    [InlineData(true, "Operator", HttpStatusCode.OK)]
    [InlineData(false, "Admin", HttpStatusCode.NotFound)]
    [InlineData(false, "Operator", HttpStatusCode.NotFound)]
    public async Task Workflow_flag_controls_masked_account_reads_without_exposing_recipient_identifiers(bool enabled, string role, HttpStatusCode expected)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(role);
        var queries = new PaymentAccountQueryProbe();
        await using var factory = AdminApiTestHost.Create(services =>
        {
            services.RemoveAll<IPaymentAccountQueryService>();
            services.AddSingleton<IPaymentAccountQueryService>(queries);
        }, configureConfiguration: configuration => configuration["Payments:Enabled"] = enabled.ToString());
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, role);

        // Act
        using var response = await client.GetAsync(new Uri("/api/v1/payment-accounts/", UriKind.Relative), TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(expected);
        var assertionTarget = response.Headers.CacheControl?.NoStore;
        assertionTarget.ShouldNotBeNull().ShouldBeTrue();
        body.ShouldNotContain("DE89370400440532013000", StringComparison.Ordinal);
        queries.SensitiveReads.ShouldBe(0);
    }

    [Theory]
    [InlineData("Admin", HttpStatusCode.OK, 1)]
    [InlineData("Operator", HttpStatusCode.Forbidden, 0)]
    public async Task Only_explicit_admin_access_invokes_sensitive_recipient_queries(string role, HttpStatusCode expected, int expectedReads)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(role);
        var queries = new PaymentAccountQueryProbe();
        await using var factory = AdminApiTestHost.Create(services =>
        {
            services.RemoveAll<IPaymentAccountQueryService>();
            services.AddSingleton<IPaymentAccountQueryService>(queries);
        }, configureConfiguration: configuration => configuration["Payments:Enabled"] = "true");
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, role);

        // Act
        using var response = await client.GetAsync(new Uri($"/api/v1/payment-accounts/{queries.Account.Id}/recipient", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(expected);
        var assertionTarget = response.Headers.CacheControl?.NoStore;
        assertionTarget.ShouldNotBeNull().ShouldBeTrue();
        queries.SensitiveReads.ShouldBe(expectedReads);
    }
}
