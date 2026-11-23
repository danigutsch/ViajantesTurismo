using ViajantesTurismo.Admin.Contracts.Application;

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
        await using var factory = queries.CreateHost(enabled);
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, role);

        // Act
        using var response = await client.GetAsync(new Uri("/api/v1/payment-accounts/", UriKind.Relative), TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var page = await client.GetAsync(new Uri("/api/v1/payment-accounts/page?page=2&pageSize=10&verifiedOnly=true", UriKind.Relative), TestContext.Current.CancellationToken);
        var pageBody = await page.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(expected);
        page.StatusCode.ShouldBe(expected);
        var assertionTarget = response.Headers.CacheControl?.NoStore;
        assertionTarget.ShouldNotBeNull().ShouldBeTrue();
        body.ShouldNotContain("DE89370400440532013000", StringComparison.Ordinal);
        var pageNoStore = page.Headers.CacheControl?.NoStore;
        pageNoStore.ShouldNotBeNull().ShouldBeTrue();
        pageBody.ShouldNotContain("DE89370400440532013000", StringComparison.Ordinal);
        queries.LastPageRequest.ShouldBe(enabled ? (2, 10, true) : null);
        queries.SensitiveReads.ShouldBe(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(ContractConstants.MaxPaymentAccountPageSize + 1)]
    public async Task Invalid_page_sizes_are_rejected_before_loading_any_account(int size)
    {
        // Arrange
        var queries = new PaymentAccountQueryProbe();
        await using var factory = queries.CreateHost(enabled: true);
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Operator");
        var path = FormattableString.Invariant($"/api/v1/payment-accounts/page?pageSize={size}");

        // Act
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        queries.LastPageRequest.ShouldBeNull();
    }

    [Fact]
    public async Task Omitted_page_arguments_use_the_default_bounded_page_without_a_status_filter()
    {
        // Arrange
        var queries = new PaymentAccountQueryProbe();
        await using var factory = queries.CreateHost(enabled: true);
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Operator");

        // Act
        using var response = await client.GetAsync(new Uri("/api/v1/payment-accounts/page", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var (page, pageSize, verifiedOnly) = queries.LastPageRequest.ShouldNotBeNull();
        page.ShouldBeNull();
        pageSize.ShouldBe(ContractConstants.DefaultPaymentAccountPageSize);
        verifiedOnly.ShouldBeFalse();
    }

    [Theory]
    [InlineData("Admin", HttpStatusCode.OK, 1)]
    [InlineData("Operator", HttpStatusCode.Forbidden, 0)]
    public async Task Only_explicit_admin_access_invokes_sensitive_recipient_queries(string role, HttpStatusCode expected, int expectedReads)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(role);
        var queries = new PaymentAccountQueryProbe();
        await using var factory = queries.CreateHost(enabled: true);
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
