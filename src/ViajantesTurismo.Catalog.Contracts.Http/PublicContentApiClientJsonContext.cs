using System.Text.Json;
using System.Text.Json.Serialization;
using SharedKernel.HttpClients;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Contracts.Http;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(ContractValidationProblemDto))]
[JsonSerializable(typeof(PublicContentDto))]
[JsonSerializable(typeof(UpsertPublicContentRequest))]
internal sealed partial class PublicContentApiClientJsonContext : JsonSerializerContext;
