using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using PandaBear.AspNetCore;
using PandaBear.Testing.AspNetCore;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.HostedTests.Management.Security;

/// <summary>Service provider hosting the management authentication services for security persistence tests.</summary>
internal sealed class ManagementSecurityPersistenceTestHost : IAsyncDisposable
{
    private const string PayloadProtectorPurpose = "ManagementSecurityPersistencePostgreSqlTests.Payload";

    private readonly ServiceProvider serviceProvider;
    private readonly IDataProtector payloadProtector;
    private readonly ITicketStore ticketStore;

    private ManagementSecurityPersistenceTestHost(ServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        payloadProtector = serviceProvider.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector(PayloadProtectorPurpose);
        ticketStore = serviceProvider.GetRequiredService<ITicketStore>();
    }

    /// <summary>Builds a host with synthetic authentication settings backed by the given connection string, optionally setting the data protection application name.</summary>
    public static ManagementSecurityPersistenceTestHost Create(
        string connectionString,
        string? applicationName = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ApiAuthenticationDefaults.AuthorityConfigurationKey] = "https://identity.example.test/realms/viajantes",
                [ApiAuthenticationDefaults.IssuerConfigurationKey] = "https://identity.example.test/realms/viajantes",
                [ManagementAuthenticationDefaults.ClientIdConfigurationKey] = "synthetic-management-client",
                [ManagementAuthenticationDefaults.ClientSecretConfigurationKey] = "synthetic-client-secret",
                [$"ConnectionStrings:{ManagementAuthenticationDefaults.SecurityDatabaseConnectionName}"] = connectionString,
                [ManagementAuthenticationDefaults.TokenExchangeEnabledConfigurationKey] = bool.TrueString,
                [ManagementAuthenticationDefaults.TokenExchangeProviderConfigurationKey] = "Keycloak"
            })
            .Build();
        var environment = new TestHostEnvironment("ViajantesTurismo.HostedTests.Management.Security")
        {
            EnvironmentName = Environments.Development
        };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddManagementAuthentication(configuration, environment);
        if (!string.IsNullOrWhiteSpace(applicationName))
        {
            services.AddDataProtection().SetApplicationName(applicationName);
        }

        return new ManagementSecurityPersistenceTestHost(
            services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateScopes = true
            }));
    }

    /// <summary>Protects the payload with the test data protector.</summary>
    public string ProtectPayload(string payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return payloadProtector.Protect(payload);
    }

    /// <summary>Unprotects a payload previously protected by this host.</summary>
    public string UnprotectPayload(string protectedPayload)
    {
        ArgumentNullException.ThrowIfNull(protectedPayload);
        return payloadProtector.Unprotect(protectedPayload);
    }

    /// <summary>Stores the authentication ticket in the ticket store and returns its key.</summary>
    public Task<string> StoreTicket(AuthenticationTicket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        return ticketStore.StoreAsync(ticket);
    }

    /// <summary>Retrieves the authentication ticket for the key, or null when none exists.</summary>
    public Task<AuthenticationTicket?> RetrieveTicket(string ticketKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ticketKey);
        return ticketStore.RetrieveAsync(ticketKey);
    }

    /// <summary>Disposes the underlying service provider.</summary>
    public ValueTask DisposeAsync() => serviceProvider.DisposeAsync();
}
