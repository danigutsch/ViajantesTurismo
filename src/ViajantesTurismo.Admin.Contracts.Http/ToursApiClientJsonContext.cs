using System.Text.Json;
using System.Text.Json.Serialization;
using SharedKernel.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Contracts.Http;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(ContractValidationProblemDto))]
[JsonSerializable(typeof(GetTourDto))]
[JsonSerializable(typeof(CreateTourDto))]
[JsonSerializable(typeof(UpdateTourDto))]
internal sealed partial class ToursApiClientJsonContext : JsonSerializerContext;
