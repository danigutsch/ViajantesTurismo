using Microsoft.Extensions.Configuration;

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
        var settings = new Dictionary<string, string?>
        {
            ["Authentication:AllowHttpDevelopmentAuthority"] = allowHttpDevelopmentAuthority.ToString()
        };

        if (includeRequiredSettings)
        {
            settings.Add("Authentication:Authority", authority);
            settings.Add("Authentication:Issuer", issuer ?? authority);
            settings.Add("Authentication:ClientId", "web-app");
            settings.Add("Authentication:ClientSecret", "client-secret");
            settings.Add("ConnectionStrings:security-database", "Host=localhost;Database=security;Username=security;Password=secret");

            if (includeTokenExchangeSettings)
            {
                settings.Add("Authentication:TokenExchange:Enabled", tokenExchangeEnabled);
                settings.Add("Authentication:TokenExchange:Provider", tokenExchangeProvider);
            }
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }
}
