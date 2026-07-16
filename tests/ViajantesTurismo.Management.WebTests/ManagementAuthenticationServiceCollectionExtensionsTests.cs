using System.Net;
using Microsoft.Extensions.Hosting;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>
/// Verifies Management Web authentication startup configuration.
/// </summary>
[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.SecurityCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.UnitScope)]
public sealed class ManagementAuthenticationServiceCollectionExtensionsTests
{
    [Fact]
    public void Production_configuration_requires_data_protection_key_encryption_settings()
    {
        // Arrange
        var configuration = ManagementAuthenticationTestConfiguration.Create();
        var environment = new TestHostEnvironment("ViajantesTurismo.Management.WebTests");

        // Act
        Action action = () => ManagementAuthenticationTestHost.Create(configuration, environment);

        // Assert
        var exception = action.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldContain("Authentication:DataProtection:CertificatePath", StringComparison.Ordinal);
    }

    [Fact]
    public void Development_configuration_requires_oidc_and_security_store_settings()
    {
        // Arrange
        var configuration = ManagementAuthenticationTestConfiguration.Create(includeRequiredSettings: false);
        var environment = new TestHostEnvironment("ViajantesTurismo.Management.WebTests") { EnvironmentName = Environments.Development };

        // Act
        Action action = () => ManagementAuthenticationTestHost.Create(configuration, environment);

        // Assert
        var exception = action.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldContain("Authentication:Authority", StringComparison.Ordinal);
    }

    [Fact]
    public void Development_configuration_requires_explicit_keycloak_token_exchange_settings()
    {
        // Arrange
        var configuration = ManagementAuthenticationTestConfiguration.Create(includeTokenExchangeSettings: false);
        var environment = new TestHostEnvironment("ViajantesTurismo.Management.WebTests") { EnvironmentName = Environments.Development };

        // Act
        Action action = () => ManagementAuthenticationTestHost.Create(configuration, environment);

        // Assert
        var exception = action.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldContain("Authentication:TokenExchange:Enabled", StringComparison.Ordinal);
    }

    [Fact]
    public void Development_configuration_rejects_unsupported_token_exchange_providers()
    {
        // Arrange
        var configuration = ManagementAuthenticationTestConfiguration.Create(tokenExchangeProvider: "Keycloak2");
        var environment = new TestHostEnvironment("ViajantesTurismo.Management.WebTests") { EnvironmentName = Environments.Development };

        // Act
        Action action = () => ManagementAuthenticationTestHost.Create(configuration, environment);

        // Assert
        action.ShouldThrow<InvalidOperationException>();
    }

    [Theory]
    [InlineData("keycloak")]
    [InlineData("KEYCLOAK")]
    public async Task Development_configuration_accepts_keycloak_token_exchange_provider_regardless_of_casing(
        string tokenExchangeProvider)
    {
        // Arrange
        var configuration = ManagementAuthenticationTestConfiguration.Create(tokenExchangeProvider: tokenExchangeProvider);
        var environment = new TestHostEnvironment("ViajantesTurismo.Management.WebTests") { EnvironmentName = Environments.Development };

        // Act
        await using var host = ManagementAuthenticationTestHost.Create(configuration, environment);

        // Assert
        host.OpenIdConnectOptions.ShouldNotBeNull();
    }

    [Fact]
    public void Development_configuration_rejects_disabled_token_exchange()
    {
        // Arrange
        var configuration = ManagementAuthenticationTestConfiguration.Create(tokenExchangeEnabled: "false");
        var environment = new TestHostEnvironment("ViajantesTurismo.Management.WebTests") { EnvironmentName = Environments.Development };

        // Act
        Action action = () => ManagementAuthenticationTestHost.Create(configuration, environment);

        // Assert
        action.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void Development_configuration_rejects_http_authority_without_the_explicit_opt_in()
    {
        // Arrange
        var configuration = ManagementAuthenticationTestConfiguration.Create(
            authority: "http://identity.example.test/realms/viajantes");
        var environment = new TestHostEnvironment("ViajantesTurismo.Management.WebTests") { EnvironmentName = Environments.Development };

        // Act
        Action action = () => ManagementAuthenticationTestHost.Create(configuration, environment);

        // Assert
        action.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void Development_configuration_rejects_unsupported_authority_schemes()
    {
        // Arrange
        var configuration = ManagementAuthenticationTestConfiguration.Create(
            allowHttpDevelopmentAuthority: true,
            authority: "ftp://identity.example.test/realms/viajantes");
        var environment = new TestHostEnvironment("ViajantesTurismo.Management.WebTests") { EnvironmentName = Environments.Development };

        // Act
        Action action = () => ManagementAuthenticationTestHost.Create(configuration, environment);

        // Assert
        action.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public async Task Development_configuration_allows_http_metadata_only_when_explicitly_enabled()
    {
        // Arrange
        var configuration = ManagementAuthenticationTestConfiguration.Create(allowHttpDevelopmentAuthority: true);
        var environment = new TestHostEnvironment("ViajantesTurismo.Management.WebTests") { EnvironmentName = Environments.Development };
        await using var host = ManagementAuthenticationTestHost.Create(configuration, environment);

        // Act
        var options = host.OpenIdConnectOptions;

        // Assert
        options.RequireHttpsMetadata.ShouldBeFalse();
        options.UsePkce.ShouldBeTrue();
        options.SaveTokens.ShouldBeFalse();
        options.EventsType.ShouldBe(typeof(ManagementOpenIdConnectEvents));
        options.Scope.ShouldContain("offline_access");
        options.Scope.ShouldContain("admin-api");
        options.Scope.ShouldContain("catalog-api");
        options.Scope.ShouldContain("branding-api");
    }

    [Fact]
    public async Task Development_configuration_requires_https_metadata_by_default()
    {
        // Arrange
        var configuration = ManagementAuthenticationTestConfiguration.Create();
        var environment = new TestHostEnvironment("ViajantesTurismo.Management.WebTests") { EnvironmentName = Environments.Development };
        await using var host = ManagementAuthenticationTestHost.Create(configuration, environment);

        // Act
        var options = host.OpenIdConnectOptions;

        // Assert
        options.RequireHttpsMetadata.ShouldBeTrue();
    }

    [Fact]
    public async Task Configures_secure_server_side_management_sessions()
    {
        // Arrange
        var configuration = ManagementAuthenticationTestConfiguration.Create();
        var environment = new TestHostEnvironment("ViajantesTurismo.Management.WebTests") { EnvironmentName = Environments.Development };
        await using var host = ManagementAuthenticationTestHost.Create(configuration, environment);

        // Act
        var cookieOptions = host.CookieOptions;
        var ticketStore = host.TicketStore;
        var userTokenStore = host.UserTokenStore;
        var protectedUserTokenStore = host.ProtectedUserTokenStore;
        var authorization = host.AuthorizationOptions;

        // Assert
        cookieOptions.Cookie.Name.ShouldBe("__Host-viajantes-management");
        cookieOptions.Cookie.HttpOnly.ShouldBeTrue();
        cookieOptions.Cookie.SecurePolicy.ShouldBe(Microsoft.AspNetCore.Http.CookieSecurePolicy.Always);
        cookieOptions.SlidingExpiration.ShouldBeFalse();
        cookieOptions.EventsType.ShouldBe(typeof(ManagementCookieAuthenticationEvents));
        ticketStore.ShouldBeOfType<ProtectedDistributedTicketStore>();
        userTokenStore.ShouldBeOfType<ProtectedDistributedUserTokenStore>();
        ReferenceEquals(userTokenStore, protectedUserTokenStore).ShouldBeTrue();
        authorization.FallbackPolicy.ShouldNotBeNull();
    }

    [Fact]
    public async Task Maps_the_user_token_store_to_the_scoped_protected_store_in_each_scope()
    {
        // Arrange
        var configuration = ManagementAuthenticationTestConfiguration.Create();
        var environment = new TestHostEnvironment("ViajantesTurismo.Management.WebTests") { EnvironmentName = Environments.Development };
        await using var host = ManagementAuthenticationTestHost.Create(configuration, environment);
        using var firstSession = host.CreateUserTokenStoreSession();
        using var secondSession = host.CreateUserTokenStoreSession();

        // Act
        var firstUserTokenStore = firstSession.UserTokenStore;
        var firstProtectedUserTokenStore = firstSession.ProtectedUserTokenStore;
        var secondUserTokenStore = secondSession.UserTokenStore;
        var secondProtectedUserTokenStore = secondSession.ProtectedUserTokenStore;

        // Assert
        ReferenceEquals(firstUserTokenStore, firstProtectedUserTokenStore).ShouldBeTrue();
        ReferenceEquals(secondUserTokenStore, secondProtectedUserTokenStore).ShouldBeTrue();
        ReferenceEquals(firstUserTokenStore, secondUserTokenStore).ShouldBeFalse();
    }

    [Fact]
    public async Task Keycloak_token_exchange_http_client_does_not_follow_temporary_redirects()
    {
        // Arrange
        var ct = Xunit.TestContext.Current.CancellationToken;
        await using var server = await RedirectingTokenExchangeTestServer.Start(ct);
        var configuration = ManagementAuthenticationTestConfiguration.Create();
        var environment = new TestHostEnvironment("ViajantesTurismo.Management.WebTests") { EnvironmentName = Environments.Development };
        await using var host = ManagementAuthenticationTestHost.Create(configuration, environment);
        using var client = host.CreateKeycloakTokenExchangeClient();
        using var requestContent = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("subject_token", "test-source-token"),
            new KeyValuePair<string, string>("client_secret", "test-client-secret")
        ]);

        // Act
        using var response = await client.PostAsync(server.TokenEndpoint, requestContent, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.TemporaryRedirect);
        server.TokenRequestBodies.Count.ShouldBe(1);
        server.TokenRequestBodies[0].ShouldContain("subject_token=test-source-token", StringComparison.Ordinal);
        server.TokenRequestBodies[0].ShouldContain("client_secret=test-client-secret", StringComparison.Ordinal);
        server.RedirectRequestBodies.ShouldBeEmpty();
    }
}
