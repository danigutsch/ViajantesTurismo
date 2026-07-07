using System.Text.Json;
using System.Text.Json.Serialization;
using SharedKernel.HttpClients;

namespace ViajantesTurismo.Admin.Contracts;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, UseStringEnumConverter = true)]
[JsonSerializable(typeof(ContractValidationProblemDto))]
[JsonSerializable(typeof(GetTourDto))]
[JsonSerializable(typeof(CreateTourDto))]
[JsonSerializable(typeof(UpdateTourDto))]
internal sealed partial class ToursApiClientJsonContext : JsonSerializerContext;
