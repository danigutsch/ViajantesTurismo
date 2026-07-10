using System.Text.Json;
using System.Text.Json.Serialization;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Contracts.Http;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(CatalogTourDto))]
[JsonSerializable(typeof(PublicContentVariantDto))]
[JsonSerializable(typeof(PublicThemeSettingsDto))]
internal sealed partial class PublicCatalogApiClientJsonContext : JsonSerializerContext;
