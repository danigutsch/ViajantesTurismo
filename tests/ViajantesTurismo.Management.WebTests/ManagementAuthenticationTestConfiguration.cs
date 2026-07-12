using Microsoft.Extensions.Configuration;

namespace ViajantesTurismo.Management.WebTests;

internal static class ManagementAuthenticationTestConfiguration
{
    public static IConfiguration Create(
        bool allowHttpDevelopmentAuthority = false,
        bool includeRequiredSettings = true)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Authentication:AllowHttpDevelopmentAuthority"] = allowHttpDevelopmentAuthority.ToString()
        };

        if (includeRequiredSettings)
        {
            settings.Add("Authentication:Authority", "https://identity.example.test/realms/viajantes");
            settings.Add("Authentication:Issuer", "https://identity.example.test/realms/viajantes");
            settings.Add("Authentication:ClientId", "web-app");
            settings.Add("Authentication:ClientSecret", "client-secret");
            settings.Add("ConnectionStrings:security-database", "Host=localhost;Database=security;Username=security;Password=secret");
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }
}
