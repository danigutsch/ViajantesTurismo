using System.Text.Json;
using System.Text.Json.Serialization;
using SharedKernel.HttpClients;

namespace ViajantesTurismo.Catalog.Contracts;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(ContractValidationProblemDto))]
[JsonSerializable(typeof(CatalogTourDto))]
[JsonSerializable(typeof(UpsertCatalogTourPresentationRequest))]
internal sealed partial class CatalogToursApiClientJsonContext : JsonSerializerContext;
