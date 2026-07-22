using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using SharedKernel.AspNetCore;
using ViajantesTurismo.Management.Security;
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
        var tokenExchangeEnabled = configuration[ManagementAuthenticationDefaults.TokenExchangeEnabledConfigurationKey];
        var tokenExchangeProvider = configuration[ManagementAuthenticationDefaults.TokenExchangeProviderConfigurationKey];

        EnsureConfigured(
            authority,
            issuer,
            clientId,
            clientSecret,
            connectionString,
            dataProtectionCertificatePath,
            dataProtectionCertificatePassword,
            allowHttpDevelopmentAuthority,
            tokenExchangeEnabled,
            tokenExchangeProvider,
            environment);

        services.AddManagementSecurityPersistence(connectionString!);
        services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString!));
        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpClient(ManagementAuthenticationDefaults.KeycloakTokenExchangeHttpClientName)
            .ConfigurePrimaryHttpMessageHandler(static () => new HttpClientHandler { AllowAutoRedirect = false });

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
        services.AddScoped<ManagementCookieAuthenticationEvents>();
        services.AddScoped<ManagementOpenIdConnectEvents>();

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
                options.EventsType = typeof(ManagementCookieAuthenticationEvents);
            })
            .AddOpenIdConnect(options =>
            {
                options.Authority = authority;
                options.ClientId = clientId;
                options.ClientSecret = clientSecret;
                options.ResponseType = "code";
                options.UsePkce = true;
                options.SaveTokens = false;
                options.EventsType = typeof(ManagementOpenIdConnectEvents);
                options.MapInboundClaims = false;
                options.TokenValidationParameters.RoleClaimType = ApiAuthenticationDefaults.RolesClaimType;
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
        services.AddOpenIdConnectAccessTokenManagement()
            .AddBlazorServerAccessTokenManagement<ProtectedDistributedUserTokenStore>();
        services.AddScoped<ProtectedDistributedAudienceTokenStore>();
        services.AddScoped<ProtectedDistributedUserTokenStore>();
        services.AddScoped<IUserTokenStore>(serviceProvider =>
            serviceProvider.GetRequiredService<ProtectedDistributedUserTokenStore>());
    }

    private static void EnsureConfigured(
        string? authority,
        string? issuer,
        string? clientId,
        string? clientSecret,
        string? connectionString,
        string? dataProtectionCertificatePath,
        string? dataProtectionCertificatePassword,
        bool allowHttpDevelopmentAuthority,
        string? tokenExchangeEnabled,
        string? tokenExchangeProvider,
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

        var allowHttpAuthority = environment.IsDevelopment() && allowHttpDevelopmentAuthority;
        ValidateAuthorityUri(authority, ApiAuthenticationDefaults.AuthorityConfigurationKey, allowHttpAuthority);
        ValidateAuthorityUri(issuer, ApiAuthenticationDefaults.IssuerConfigurationKey, allowHttpAuthority);
        ValidateTokenExchangeConfiguration(tokenExchangeEnabled, tokenExchangeProvider);

        if (!environment.IsDevelopment()
            && (string.IsNullOrWhiteSpace(dataProtectionCertificatePath)
                || string.IsNullOrWhiteSpace(dataProtectionCertificatePassword)))
        {
            throw new InvalidOperationException(
                "Authentication:DataProtection:CertificatePath and "
                + "Authentication:DataProtection:CertificatePassword must be set outside Development.");
        }
    }

    private static void ValidateTokenExchangeConfiguration(string? tokenExchangeEnabled, string? tokenExchangeProvider)
    {
        if (!bool.TryParse(tokenExchangeEnabled, out var enabled) || !enabled)
        {
            throw new InvalidOperationException(
                $"{ManagementAuthenticationDefaults.TokenExchangeEnabledConfigurationKey} must be explicitly set to true.");
        }

        if (!string.Equals(
                tokenExchangeProvider,
                ManagementAuthenticationDefaults.KeycloakTokenExchangeProvider,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{ManagementAuthenticationDefaults.TokenExchangeProviderConfigurationKey} must be {ManagementAuthenticationDefaults.KeycloakTokenExchangeProvider}.");
        }
    }

    private static void ValidateAuthorityUri(string value, string configurationKey, bool allowHttpAuthority)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException(
                $"{configurationKey} must be an HTTPS absolute URI outside an explicitly configured Development environment.");
        }

        var isHttps = string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        var isHttp = string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);
        if (!isHttps && !(allowHttpAuthority && isHttp))
        {
            throw new InvalidOperationException(
                $"{configurationKey} must be an HTTPS absolute URI outside an explicitly configured Development environment.");
        }
    }
}
