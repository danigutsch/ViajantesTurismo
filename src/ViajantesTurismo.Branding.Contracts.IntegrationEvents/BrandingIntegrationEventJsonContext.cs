using System.Text.Json.Serialization;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents.Branding;

namespace ViajantesTurismo.Branding.Contracts.IntegrationEvents;

/// <inheritdoc />
[JsonSerializable(typeof(BrandingSettingsChangedIntegrationEvent))]
public sealed partial class BrandingIntegrationEventJsonContext : JsonSerializerContext;
