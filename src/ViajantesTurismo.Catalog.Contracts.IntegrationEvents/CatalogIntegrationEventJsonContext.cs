using System.Text.Json.Serialization;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents.Media;

namespace ViajantesTurismo.Catalog.Contracts.IntegrationEvents;

/// <inheritdoc/>
[JsonSerializable(typeof(MediaImageOriginalStoredIntegrationEvent))]
public sealed partial class CatalogIntegrationEventJsonContext : JsonSerializerContext;
