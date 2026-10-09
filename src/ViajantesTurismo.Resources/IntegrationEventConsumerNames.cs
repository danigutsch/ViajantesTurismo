namespace ViajantesTurismo.Resources;

/// <summary>
/// Contains stable integration-event consumer queue names.
/// </summary>
public static class IntegrationEventConsumerNames
{
    /// <summary>
    /// Admin bounded-context consumer queue name.
    /// </summary>
    public const string Admin = "admin";

    /// <summary>
    /// Catalog bounded-context consumer queue name.
    /// </summary>
    public const string Catalog = "catalog";
}
