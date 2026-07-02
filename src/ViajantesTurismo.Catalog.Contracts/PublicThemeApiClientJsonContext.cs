using System.Text.Json;
using System.Text.Json.Serialization;
using ViajantesTurismo.Common.Contracts;

namespace ViajantesTurismo.Catalog.Contracts;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(ContractValidationProblemDto))]
[JsonSerializable(typeof(PublicThemeSettingsDto))]
internal sealed partial class PublicThemeApiClientJsonContext : JsonSerializerContext;
