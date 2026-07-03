using System.Text.Json;
using System.Text.Json.Serialization;
using SharedKernel.HttpClients;

namespace ViajantesTurismo.Catalog.Contracts;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(ContractValidationProblemDto))]
[JsonSerializable(typeof(PublicContentDto))]
[JsonSerializable(typeof(UpsertPublicContentRequest))]
internal sealed partial class PublicContentApiClientJsonContext : JsonSerializerContext;
