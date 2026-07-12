using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using SharedKernel.AspNetCore;
using System.Security.Cryptography.X509Certificates;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Management.Web;

/// <summary>
/// Configures the Management Web confidential OIDC BFF boundary.
/// </summary>
internal static class ManagementAuthenticationServiceCollectionExtensions
{
    internal static void AddManagementAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var authority = configuration[ApiAuthenticationDefaults.AuthorityConfigurationKey];
        var issuer = configuration[ApiAuthenticationDefaults.IssuerConfigurationKey];
        var clientId = configuration[ManagementAuthenticationDefaults.ClientIdConfigurationKey];
        var clientSecret = configuration[ManagementAuthenticationDefaults.ClientSecretConfigurationKey];
        var connectionString = configuration.GetConnectionString(ManagementAuthenticationDefaults.SecurityDatabaseConnectionName);
        var dataProtectionCertificatePath = configuration[ManagementAuthenticationDefaults.DataProtectionCertificatePathConfigurationKey];
        var dataProtectionCertificatePassword = configuration[ManagementAuthenticationDefaults.DataProtectionCertificatePasswordConfigurationKey];
        var allowHttpDevelopmentAuthority = configuration.GetValue<bool>(ApiAuthenticationDefaults.AllowHttpDevelopmentAuthorityConfigurationKey);

        EnsureConfigured(
            authority,
            issuer,
            clientId,
            clientSecret,
            connectionString,
            dataProtectionCertificatePath,
            dataProtectionCertificatePassword,
            environment);

        services.AddDistributedPostgresCache(options =>
        {
            options.ConnectionString = connectionString;
            options.SchemaName = ManagementAuthenticationDefaults.SecurityStoreSchemaName;
            options.TableName = ManagementAuthenticationDefaults.TicketStoreTableName;
            options.CreateIfNotExists = environment.IsDevelopment();
            options.UseWAL = true;
        });
        services.AddDbContext<ManagementSecurityDbContext>(options => options.UseNpgsql(connectionString));

        var dataProtection = services.AddDataProtection()
            .PersistKeysToDbContext<ManagementSecurityDbContext>()
            .SetApplicationName("ViajantesTurismo.Management.Web");
        if (!string.IsNullOrWhiteSpace(dataProtectionCertificatePath))
        {
            var certificate = X509CertificateLoader.LoadPkcs12FromFile(
                dataProtectionCertificatePath,
                dataProtectionCertificatePassword);
            dataProtection.ProtectKeysWithCertificate(certificate);
        }
        services.AddSingleton<ITicketStore, ProtectedDistributedTicketStore>();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.Name = ManagementAuthenticationDefaults.CookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.Path = "/";
                options.ExpireTimeSpan = ManagementAuthenticationDefaults.SessionLifetime;
                options.SlidingExpiration = false;
            })
            .AddOpenIdConnect(options =>
            {
                options.Authority = authority;
                options.ClientId = clientId;
                options.ClientSecret = clientSecret;
                options.ResponseType = "code";
                options.UsePkce = true;
                options.SaveTokens = true;
                options.MapInboundClaims = false;
                options.RequireHttpsMetadata = !(environment.IsDevelopment() && allowHttpDevelopmentAuthority);
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("offline_access");
                options.Scope.Add(ApiAudienceNames.Admin);
                options.Scope.Add(ApiAudienceNames.Catalog);
                options.Scope.Add(ApiAudienceNames.Branding);

                if (!string.IsNullOrWhiteSpace(issuer))
                {
                    options.TokenValidationParameters.ValidIssuer = issuer;
                }
            });

        services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<ITicketStore>((options, ticketStore) => options.SessionStore = ticketStore);
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());
        services.AddOpenIdConnectAccessTokenManagement();
    }

    private static void EnsureConfigured(
        string? authority,
        string? issuer,
        string? clientId,
        string? clientSecret,
        string? connectionString,
        string? dataProtectionCertificatePath,
        string? dataProtectionCertificatePassword,
        IHostEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(authority)
            || string.IsNullOrWhiteSpace(issuer)
            || string.IsNullOrWhiteSpace(clientId)
            || string.IsNullOrWhiteSpace(clientSecret)
            || string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Authentication:Authority, Authentication:Issuer, Authentication:ClientId, "
                + "Authentication:ClientSecret, and ConnectionStrings:security-database must be set.");
        }

        if (!environment.IsDevelopment()
            && (string.IsNullOrWhiteSpace(dataProtectionCertificatePath)
                || string.IsNullOrWhiteSpace(dataProtectionCertificatePassword)))
        {
            throw new InvalidOperationException(
                "Authentication:DataProtection:CertificatePath and "
                + "Authentication:DataProtection:CertificatePassword must be set outside Development.");
        }
    }
}
