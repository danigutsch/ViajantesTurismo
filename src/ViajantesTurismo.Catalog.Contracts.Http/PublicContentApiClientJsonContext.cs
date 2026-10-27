using System.Text.Json;
using System.Text.Json.Serialization;
using PandaBear.HttpClients;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Contracts.Http;

/// <summary>Source-generated JSON serialization context for the public content API client.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(ContractValidationProblemDto))]
[JsonSerializable(typeof(PublicContentDto))]
[JsonSerializable(typeof(PublicContentDto[]))]
[JsonSerializable(typeof(UpsertPublicContentRequest))]
internal sealed partial class PublicContentApiClientJsonContext : JsonSerializerContext;
