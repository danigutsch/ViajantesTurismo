using System.Text.Json.Serialization;
using ViajantesTurismo.Admin.Contracts.Tours;
using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.Catalog.Infrastructure;

[JsonSerializable(typeof(AdminTourCreatedIntegrationEvent))]
[JsonSerializable(typeof(MediaImageOriginalStoredIntegrationEvent))]
internal sealed partial class CatalogIntegrationEventJsonContext : JsonSerializerContext;
