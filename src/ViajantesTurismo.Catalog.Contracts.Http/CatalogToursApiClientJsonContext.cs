using System.Text.Json;
using System.Text.Json.Serialization;
using PandaBear.HttpClients;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Contracts.Http;

/// <summary>Source-generated JSON serialization context for the Catalog tours API client.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(ContractValidationProblemDto))]
[JsonSerializable(typeof(CatalogTourDto))]
[JsonSerializable(typeof(CatalogTourDto[]))]
[JsonSerializable(typeof(UpsertCatalogTourPresentationRequest))]
[JsonSerializable(typeof(CatalogTourPublicationRequest))]
[JsonSerializable(typeof(PublicMediaImageAccessibilityDraftRequest))]
[JsonSerializable(typeof(CatalogMediaImageDto))]
[JsonSerializable(typeof(CatalogMediaImageDto[]))]
[JsonSerializable(typeof(CatalogMediaImageVariantDto))]
[JsonSerializable(typeof(PublicMediaImageAccessibilityReviewRequest))]
internal sealed partial class CatalogToursApiClientJsonContext : JsonSerializerContext;
