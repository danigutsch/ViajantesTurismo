using Microsoft.Extensions.Configuration;
using SharedKernel.AspNetCore;

namespace ViajantesTurismo.Management.WebTests;

internal static class ManagementAuthenticationTestConfiguration
{
    public static IConfiguration Create(
        bool allowHttpDevelopmentAuthority = false,
        bool includeRequiredSettings = true,
        string authority = "https://identity.example.test/realms/viajantes",
        string? issuer = null,
        bool includeTokenExchangeSettings = true,
        string tokenExchangeEnabled = "true",
        string tokenExchangeProvider = "Keycloak")
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(CreateSettings(
                allowHttpDevelopmentAuthority,
                includeRequiredSettings,
                authority,
                issuer,
                includeTokenExchangeSettings,
                tokenExchangeEnabled,
                tokenExchangeProvider))
            .Build();
    }

    public static Dictionary<string, string?> CreateSettings(
        bool allowHttpDevelopmentAuthority = false,
        bool includeRequiredSettings = true,
        string authority = "https://identity.example.test/realms/viajantes",
        string? issuer = null,
        bool includeTokenExchangeSettings = true,
        string tokenExchangeEnabled = "true",
        string tokenExchangeProvider = "Keycloak")
    {
        var settings = new Dictionary<string, string?>
        {
            [ApiAuthenticationDefaults.AllowHttpDevelopmentAuthorityConfigurationKey] = allowHttpDevelopmentAuthority.ToString()
        };

        if (includeRequiredSettings)
        {
            settings.Add(ApiAuthenticationDefaults.AuthorityConfigurationKey, authority);
            settings.Add(ApiAuthenticationDefaults.IssuerConfigurationKey, issuer ?? authority);
            settings.Add("Authentication:ClientId", "web-app");
            settings.Add("Authentication:ClientSecret", "client-secret");
            settings.Add("ConnectionStrings:security-database", "Host=localhost;Database=security;Username=security;Password=secret");

            if (includeTokenExchangeSettings)
            {
                settings.Add("Authentication:TokenExchange:Enabled", tokenExchangeEnabled);
                settings.Add("Authentication:TokenExchange:Provider", tokenExchangeProvider);
            }
        }

        return settings;
    }
}
