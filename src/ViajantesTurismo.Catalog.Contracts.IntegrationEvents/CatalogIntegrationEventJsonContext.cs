using System.Text.Json.Serialization;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents.Media;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents.Tours;

namespace ViajantesTurismo.Catalog.Contracts.IntegrationEvents;

/// <inheritdoc/>
[JsonSerializable(typeof(MediaImageOriginalStoredIntegrationEvent))]
[JsonSerializable(typeof(CatalogTourRetirementAppliedIntegrationEvent))]
[JsonSerializable(typeof(CatalogGalleryPlacementQueuedIntegrationEvent))]
public sealed partial class CatalogIntegrationEventJsonContext : JsonSerializerContext;
