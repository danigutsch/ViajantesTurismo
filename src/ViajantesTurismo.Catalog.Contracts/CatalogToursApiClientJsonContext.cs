using System.Text.Json;
using System.Text.Json.Serialization;
using SharedKernel.HttpClients;

namespace ViajantesTurismo.Catalog.Contracts;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(ContractValidationProblemDto))]
[JsonSerializable(typeof(CatalogTourDto))]
[JsonSerializable(typeof(UpsertCatalogTourPresentationRequest))]
[JsonSerializable(typeof(PublicMediaImageAccessibilityDraftRequest))]
[JsonSerializable(typeof(PublicMediaImageDto))]
internal sealed partial class CatalogToursApiClientJsonContext : JsonSerializerContext;
