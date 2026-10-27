using System.Text.Json;
using System.Text.Json.Serialization;
using PandaBear.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>Source-generated JSON serialization context for the Tours API client.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(ContractValidationProblemDto))]
[JsonSerializable(typeof(GetTourDto))]
[JsonSerializable(typeof(GetTourDto[]))]
[JsonSerializable(typeof(CreateTourDto))]
[JsonSerializable(typeof(UpdateTourDto))]
internal sealed partial class ToursApiClientJsonContext : JsonSerializerContext;
