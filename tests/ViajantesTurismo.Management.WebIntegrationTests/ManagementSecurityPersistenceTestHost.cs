using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SharedKernel.AspNetCore;
using SharedKernel.Testing.AspNetCore;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebIntegrationTests;

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
        var environment = new TestHostEnvironment("ViajantesTurismo.Management.WebIntegrationTests")
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

    public string ProtectPayload(string payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return payloadProtector.Protect(payload);
    }

    public string UnprotectPayload(string protectedPayload)
    {
        ArgumentNullException.ThrowIfNull(protectedPayload);
        return payloadProtector.Unprotect(protectedPayload);
    }

    public Task<string> StoreTicket(AuthenticationTicket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        return ticketStore.StoreAsync(ticket);
    }

    public Task<AuthenticationTicket?> RetrieveTicket(string ticketKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ticketKey);
        return ticketStore.RetrieveAsync(ticketKey);
    }

    public ValueTask DisposeAsync() => serviceProvider.DisposeAsync();
}
