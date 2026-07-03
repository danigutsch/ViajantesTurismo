using System.Text.Json;
using System.Text.Json.Serialization;
using ViajantesTurismo.Common.Contracts;

namespace ViajantesTurismo.Admin.Contracts;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(ContractValidationProblemDto))]
[JsonSerializable(typeof(GetTourDto))]
[JsonSerializable(typeof(CreateTourDto))]
[JsonSerializable(typeof(UpdateTourDto))]
internal sealed partial class ToursApiClientJsonContext : JsonSerializerContext;
