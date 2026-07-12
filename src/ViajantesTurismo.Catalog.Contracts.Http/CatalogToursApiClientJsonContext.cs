using System.Text.Json;
using System.Text.Json.Serialization;
using SharedKernel.HttpClients;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Contracts.Http;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(ContractValidationProblemDto))]
[JsonSerializable(typeof(CatalogTourDto))]
[JsonSerializable(typeof(UpsertCatalogTourPresentationRequest))]
[JsonSerializable(typeof(PublicMediaImageAccessibilityDraftRequest))]
[JsonSerializable(typeof(PublicMediaImageDto))]
[JsonSerializable(typeof(PublicMediaImageDto[]))]
[JsonSerializable(typeof(PublicMediaImageAccessibilityReviewRequest))]
internal sealed partial class CatalogToursApiClientJsonContext : JsonSerializerContext;
