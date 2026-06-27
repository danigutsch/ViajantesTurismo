namespace ViajantesTurismo.Catalog.Application.IntegrationEvents;

/// <summary>
/// Configures Catalog integration event handling policies.
/// </summary>
public sealed class CatalogIntegrationEventOptions
{
    /// <summary>
    /// Configuration section name for Catalog integration event handling.
    /// </summary>
    public const string SectionName = "CatalogIntegrationEvents";

    /// <summary>
    /// Gets or sets the duration for holding an idempotency lock while handling an integration event.
    /// </summary>
    public TimeSpan IdempotencyLockDuration { get; set; } = TimeSpan.FromMinutes(5);
}
