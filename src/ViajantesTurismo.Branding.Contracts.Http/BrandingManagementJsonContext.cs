using System.Text.Json;
using System.Text.Json.Serialization;
using PandaBear.Branding;
using PandaBear.HttpClients;

namespace ViajantesTurismo.Branding.Contracts.Http;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(ContractValidationProblemDto))]
[JsonSerializable(typeof(BrandingSettingsDto))]
[JsonSerializable(typeof(BrandingManagementSettingsDto))]
internal sealed partial class BrandingManagementJsonContext : JsonSerializerContext;
