using Microsoft.Extensions.Hosting;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>
/// Verifies Management Web authentication startup configuration.
/// </summary>
public sealed class ManagementAuthenticationServiceCollectionExtensionsTests
{
    [Fact]
    public void Production_configuration_requires_data_protection_key_encryption_settings()
    {
        // Arrange
        var configuration = ManagementAuthenticationTestConfiguration.Create();
        var environment = new TestHostEnvironment();

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
        var environment = new TestHostEnvironment { EnvironmentName = Environments.Development };

        // Act
        Action action = () => ManagementAuthenticationTestHost.Create(configuration, environment);

        // Assert
        var exception = action.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldContain("Authentication:Authority", StringComparison.Ordinal);
    }

    [Fact]
    public void Development_configuration_rejects_http_authority_without_the_explicit_opt_in()
    {
        // Arrange
        var configuration = ManagementAuthenticationTestConfiguration.Create(
            authority: "http://identity.example.test/realms/viajantes");
        var environment = new TestHostEnvironment { EnvironmentName = Environments.Development };

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
        var environment = new TestHostEnvironment { EnvironmentName = Environments.Development };

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
        var environment = new TestHostEnvironment { EnvironmentName = Environments.Development };
        await using var host = ManagementAuthenticationTestHost.Create(configuration, environment);

        // Act
        var options = host.OpenIdConnectOptions;

        // Assert
        options.RequireHttpsMetadata.ShouldBeFalse();
        options.UsePkce.ShouldBeTrue();
        options.SaveTokens.ShouldBeTrue();
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
        var environment = new TestHostEnvironment { EnvironmentName = Environments.Development };
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
        var environment = new TestHostEnvironment { EnvironmentName = Environments.Development };
        await using var host = ManagementAuthenticationTestHost.Create(configuration, environment);

        // Act
        var cookieOptions = host.CookieOptions;
        var ticketStore = host.TicketStore;
        var authorization = host.AuthorizationOptions;

        // Assert
        cookieOptions.Cookie.Name.ShouldBe("__Host-viajantes-management");
        cookieOptions.Cookie.HttpOnly.ShouldBeTrue();
        cookieOptions.Cookie.SecurePolicy.ShouldBe(Microsoft.AspNetCore.Http.CookieSecurePolicy.Always);
        cookieOptions.SlidingExpiration.ShouldBeFalse();
        ticketStore.ShouldBeOfType<ProtectedDistributedTicketStore>();
        authorization.FallbackPolicy.ShouldNotBeNull();
    }
}
