using System.Text.Json;
using System.Text.Json.Serialization;
using PandaBear.Branding;
using PandaBear.HttpClients;

namespace ViajantesTurismo.Branding.Contracts.Http;

/// <summary>Source-generated JSON serialization context for the Branding management API client.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(ContractValidationProblemDto))]
[JsonSerializable(typeof(BrandingSettingsDto))]
[JsonSerializable(typeof(BrandingManagementSettingsDto))]
internal sealed partial class BrandingManagementJsonContext : JsonSerializerContext;
