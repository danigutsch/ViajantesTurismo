namespace ViajantesTurismo.Management.Web;

/// <summary>
/// Adds a Keycloak token-exchange handler for a single protected backend audience.
/// </summary>
internal static class ManagementAudienceTokenExchangeHttpClientBuilderExtensions
{
    internal static IHttpClientBuilder AddKeycloakAudienceTokenExchangeHandler(this IHttpClientBuilder builder, string audience)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(audience);

        return builder.AddHttpMessageHandler(serviceProvider =>
            ActivatorUtilities.CreateInstance<KeycloakAudienceTokenExchangeHandler>(serviceProvider, audience));
    }
}
