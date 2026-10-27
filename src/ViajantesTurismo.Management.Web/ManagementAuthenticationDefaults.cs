namespace ViajantesTurismo.Management.Web;

/// <summary>
/// Defines Management Web authentication configuration and session conventions.
/// </summary>
internal static class ManagementAuthenticationDefaults
{
    /// <summary>
    /// Configuration key for the OIDC client identifier.
    /// </summary>
    internal const string ClientIdConfigurationKey = "Authentication:ClientId";
    /// <summary>
    /// Configuration key for the OIDC client secret.
    /// </summary>
    internal const string ClientSecretConfigurationKey = "Authentication:ClientSecret";
    /// <summary>
    /// Configuration key that toggles backend token exchange.
    /// </summary>
    internal const string TokenExchangeEnabledConfigurationKey = "Authentication:TokenExchange:Enabled";
    /// <summary>
    /// Configuration key that selects the token exchange provider.
    /// </summary>
    internal const string TokenExchangeProviderConfigurationKey = "Authentication:TokenExchange:Provider";
    /// <summary>
    /// Provider name for Keycloak token exchange.
    /// </summary>
    internal const string KeycloakTokenExchangeProvider = "Keycloak";
    /// <summary>
    /// Named HTTP client used for Keycloak token exchange requests.
    /// </summary>
    internal const string KeycloakTokenExchangeHttpClientName = "ManagementKeycloakTokenExchange";
    /// <summary>
    /// Connection string name of the security database.
    /// </summary>
    internal const string SecurityDatabaseConnectionName = "security-database";
    /// <summary>
    /// Distributed cache key prefix for stored cookie authentication tickets.
    /// </summary>
    internal const string TicketStoreKeyPrefix = "management-ticket:";
    /// <summary>
    /// Data protection purpose for encrypting stored cookie tickets.
    /// </summary>
    internal const string TicketStoreProtectorPurpose = "ViajantesTurismo.Management.Web.CookieTicketStore.v1";
    /// <summary>
    /// Distributed cache key prefix for stored user tokens.
    /// </summary>
    internal const string UserTokenStoreKeyPrefix = "management-user-token:";
    /// <summary>
    /// Data protection purpose for encrypting stored user tokens.
    /// </summary>
    internal const string UserTokenStoreProtectorPurpose = "ViajantesTurismo.Management.Web.UserTokenStore.v1";
    /// <summary>
    /// Claim type carrying the opaque token session identifier.
    /// </summary>
    internal const string UserTokenStoreSessionIdClaimType = "urn:viajantes:management:token-session-id";
    /// <summary>
    /// Claim type carrying the absolute token session expiry.
    /// </summary>
    internal const string UserTokenStoreSessionExpiresAtClaimType = "urn:viajantes:management:token-session-expires-at";
    /// <summary>
    /// Name of the authentication cookie; the __Host- prefix requires Secure, Path=/ and no Domain.
    /// </summary>
    internal const string CookieName = "__Host-viajantes-management";
    /// <summary>
    /// Configuration key for the data protection certificate file path.
    /// </summary>
    internal const string DataProtectionCertificatePathConfigurationKey = "Authentication:DataProtection:CertificatePath";
    /// <summary>
    /// Configuration key for the data protection certificate password.
    /// </summary>
    internal const string DataProtectionCertificatePasswordConfigurationKey = "Authentication:DataProtection:CertificatePassword";
    /// <summary>
    /// Path that starts the OIDC login challenge.
    /// </summary>
    internal const string LoginPath = "/login";
    /// <summary>
    /// Path that signs the user out.
    /// </summary>
    internal const string LogoutPath = "/logout";
    /// <summary>
    /// Absolute lifetime of a Management login session.
    /// </summary>
    internal static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);
}
