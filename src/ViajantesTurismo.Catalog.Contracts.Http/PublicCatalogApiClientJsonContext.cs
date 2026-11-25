using System.Text.Json;
using System.Text.Json.Serialization;
using SharedKernel.HttpClients;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Contracts.Http;

/// <summary>Source-generated JSON serialization context for the public Catalog API client.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(ContractValidationProblemDto))]
[JsonSerializable(typeof(PublishedTourPageDto))]
[JsonSerializable(typeof(PublishedTourSitemapEntryDto[]))]
[JsonSerializable(typeof(PublishedTourDetailsPageDto))]
[JsonSerializable(typeof(CatalogGalleryIndexDto))]
[JsonSerializable(typeof(CatalogGalleryAlbumDto))]
[JsonSerializable(typeof(PublicContentVariantDto))]
internal sealed partial class PublicCatalogApiClientJsonContext : JsonSerializerContext;
