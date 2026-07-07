using System.Text.Json;
using System.Text.Json.Serialization;

namespace ViajantesTurismo.Catalog.Contracts;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, UseStringEnumConverter = true)]
[JsonSerializable(typeof(CatalogTourDto))]
[JsonSerializable(typeof(PublicContentVariantDto))]
[JsonSerializable(typeof(PublicThemeSettingsDto))]
internal sealed partial class PublicCatalogApiClientJsonContext : JsonSerializerContext;
