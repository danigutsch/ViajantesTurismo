namespace ViajantesTurismo.Management.Web;

/// <summary>
/// Defines Management Web authentication configuration and session conventions.
/// </summary>
internal static class ManagementAuthenticationDefaults
{
    internal const string ClientIdConfigurationKey = "Authentication:ClientId";
    internal const string ClientSecretConfigurationKey = "Authentication:ClientSecret";
    internal const string TokenExchangeEnabledConfigurationKey = "Authentication:TokenExchange:Enabled";
    internal const string TokenExchangeProviderConfigurationKey = "Authentication:TokenExchange:Provider";
    internal const string KeycloakTokenExchangeProvider = "Keycloak";
    internal const string KeycloakTokenExchangeHttpClientName = "ManagementKeycloakTokenExchange";
    internal const string SecurityDatabaseConnectionName = "security-database";
    internal const string TicketStoreKeyPrefix = "management-ticket:";
    internal const string TicketStoreProtectorPurpose = "ViajantesTurismo.Management.Web.CookieTicketStore.v1";
    internal const string UserTokenStoreKeyPrefix = "management-user-token:";
    internal const string UserTokenStoreProtectorPurpose = "ViajantesTurismo.Management.Web.UserTokenStore.v1";
    internal const string UserTokenStoreSessionIdClaimType = "urn:viajantes:management:token-session-id";
    internal const string UserTokenStoreSessionExpiresAtClaimType = "urn:viajantes:management:token-session-expires-at";
    internal const string CookieName = "__Host-viajantes-management";
    internal const string DataProtectionCertificatePathConfigurationKey = "Authentication:DataProtection:CertificatePath";
    internal const string DataProtectionCertificatePasswordConfigurationKey = "Authentication:DataProtection:CertificatePassword";
    internal const string LoginPath = "/login";
    internal const string LogoutPath = "/logout";
    internal static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);
}
