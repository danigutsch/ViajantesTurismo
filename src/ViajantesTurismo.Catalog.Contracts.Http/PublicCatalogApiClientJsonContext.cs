using System.Text.Json;
using System.Text.Json.Serialization;
using PandaBear.HttpClients;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Contracts.Http;

/// <summary>Source-generated JSON serialization context for the public Catalog API client.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(ContractValidationProblemDto))]
[JsonSerializable(typeof(TourSummaryDto))]
[JsonSerializable(typeof(TourSummaryDto[]))]
[JsonSerializable(typeof(TourDetailsDto))]
[JsonSerializable(typeof(PublicContentVariantDto))]
internal sealed partial class PublicCatalogApiClientJsonContext : JsonSerializerContext;
