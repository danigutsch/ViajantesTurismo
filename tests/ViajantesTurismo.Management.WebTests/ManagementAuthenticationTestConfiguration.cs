using Microsoft.Extensions.Configuration;

namespace ViajantesTurismo.Management.WebTests;

internal static class ManagementAuthenticationTestConfiguration
{
    public static IConfiguration Create(bool allowHttpDevelopmentAuthority = false)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Authority"] = "https://identity.example.test/realms/viajantes",
                ["Authentication:Issuer"] = "https://identity.example.test/realms/viajantes",
                ["Authentication:ClientId"] = "web-app",
                ["Authentication:ClientSecret"] = "client-secret",
                ["Authentication:AllowHttpDevelopmentAuthority"] = allowHttpDevelopmentAuthority.ToString(),
                ["ConnectionStrings:security-database"] = "Host=localhost;Database=security;Username=security;Password=secret"
            })
            .Build();
    }
}
