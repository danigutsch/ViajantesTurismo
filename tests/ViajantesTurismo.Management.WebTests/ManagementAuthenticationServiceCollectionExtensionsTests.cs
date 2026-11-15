using PandaBear.Testing;
using Microsoft.Extensions.Hosting;
using PandaBear.AspNetCore.Authentication;
using PandaBear.AspNetCore.Authentication.Bff;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>
/// Verifies Management Web authentication startup configuration.
/// </summary>
[Trait(TestTraitNames.CategoryName, TestTraits.SecurityCategory)]
[TestScope(TestScope.Integration)]
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
    public void Development_configuration_requires_OIDC_and_security_store_settings()
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
    public void Development_configuration_rejects_HTTP_authority_without_the_explicit_opt_in()
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
    public async Task Development_configuration_allows_HTTP_metadata_only_when_explicitly_enabled()
    {
        // Arrange
        var configuration = ManagementAuthenticationTestConfiguration.Create(allowHttpDevelopmentAuthority: true);
        var environment = new TestHostEnvironment("ViajantesTurismo.Management.WebTests") { EnvironmentName = Environments.Development };
        await using var host = ManagementAuthenticationTestHost.Create(configuration, environment);

        // Act
        var options = host.OpenIdConnectOptions;

        // Assert
        options.RequireHttpsMetadata.ShouldBeFalse();
        options.TokenValidationParameters.RoleClaimType.ShouldBe(ApiAuthenticationDefaults.RolesClaimType);
        options.UsePkce.ShouldBeTrue();
        options.SaveTokens.ShouldBeFalse();
        options.EventsType.ShouldBeNull();
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
    public async Task Names_the_advisory_lock_data_source_for_pool_metrics()
    {
        // Arrange
        var configuration = ManagementAuthenticationTestConfiguration.Create();
        var environment = new TestHostEnvironment("ViajantesTurismo.Management.WebTests") { EnvironmentName = Environments.Development };
        await using var host = ManagementAuthenticationTestHost.Create(configuration, environment);

        // Act
        host.ResolveAdvisoryLockDataSource();
        var poolNames = NpgsqlPoolNameObserver.Observe();

        // Assert
        poolNames.ShouldContain("management-advisory-lock");
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
        var ticketStoreOptions = host.TicketStoreOptions;
        var authorization = host.AuthorizationOptions;

        // Assert
        cookieOptions.Cookie.Name.ShouldBe("__Host-viajantes-management");
        cookieOptions.Cookie.HttpOnly.ShouldBeTrue();
        cookieOptions.Cookie.SecurePolicy.ShouldBe(Microsoft.AspNetCore.Http.CookieSecurePolicy.Always);
        cookieOptions.Cookie.SameSite.ShouldBe(Microsoft.AspNetCore.Http.SameSiteMode.Lax);
        cookieOptions.ExpireTimeSpan.ShouldBe(TimeSpan.FromHours(8));
        cookieOptions.SlidingExpiration.ShouldBeFalse();
        cookieOptions.EventsType.ShouldBeNull();
        cookieOptions.SessionStore.ShouldBeOfType<ServerSideTicketStore>();
        ticketStoreOptions.KeyPrefix.ShouldBe("management-ticket:");
        ticketStoreOptions.AbsoluteLifetime.ShouldBe(TimeSpan.FromHours(8));
        ticketStoreOptions.SlidingExpiration.ShouldBeNull();
        authorization.FallbackPolicy.ShouldNotBeNull();
    }

    [Fact]
    public async Task Keeps_user_tokens_server_side_for_the_session_lifetime_and_locks_sessions_in_postgresql()
    {
        // Arrange
        var configuration = ManagementAuthenticationTestConfiguration.Create();
        var environment = new TestHostEnvironment("ViajantesTurismo.Management.WebTests") { EnvironmentName = Environments.Development };
        await using var host = ManagementAuthenticationTestHost.Create(configuration, environment);

        // Act
        var tokenOptions = host.UserTokenOptions;
        var sessionLock = host.SessionLock;
        var tokenManager = host.UserTokenManager;

        // Assert
        tokenOptions.CacheKeyPrefix.ShouldBe("management-user-token:");
        tokenOptions.SessionLifetime.ShouldBe(TimeSpan.FromHours(8));
        tokenOptions.RevokeRefreshTokenOnSignOut.ShouldBeTrue();
        tokenOptions.RejectCookieWithoutTokenSession.ShouldBeTrue();
        sessionLock.ShouldBeOfType<PostgreSqlUserSessionLock>();
        tokenManager.ShouldNotBeNull();
    }
}
