using System.Text.Json.Serialization;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;

namespace ViajantesTurismo.Admin.Contracts.IntegrationEvents;

/// <inheritdoc/>
[JsonSerializable(typeof(AdminTourCreatedIntegrationEvent))]
[JsonSerializable(typeof(AdminTourRetiredIntegrationEvent))]
public sealed partial class AdminIntegrationEventJsonContext : JsonSerializerContext;
