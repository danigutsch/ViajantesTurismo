using System.Text.Json.Serialization;
using ViajantesTurismo.Catalog.Contracts.Media;

namespace ViajantesTurismo.Catalog.Contracts;

/// <inheritdoc/>
[JsonSerializable(typeof(MediaImageOriginalStoredIntegrationEvent))]
public sealed partial class CatalogIntegrationEventJsonContext : JsonSerializerContext;
