using System.Text.Json.Serialization;
using ViajantesTurismo.Admin.Contracts.Tours;

namespace ViajantesTurismo.Admin.Infrastructure;

[JsonSerializable(typeof(AdminTourCreatedIntegrationEvent))]
internal sealed partial class AdminIntegrationEventJsonContext : JsonSerializerContext;
