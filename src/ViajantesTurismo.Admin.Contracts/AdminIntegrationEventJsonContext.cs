using System.Text.Json.Serialization;
using ViajantesTurismo.Admin.Contracts.Tours;

namespace ViajantesTurismo.Admin.Contracts;

/// <inheritdoc/>
[JsonSerializable(typeof(AdminTourCreatedIntegrationEvent))]
public sealed partial class AdminIntegrationEventJsonContext : JsonSerializerContext;
