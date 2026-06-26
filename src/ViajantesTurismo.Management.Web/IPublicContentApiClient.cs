using ViajantesTurismo.Catalog.Contracts;

namespace ViajantesTurismo.Management.Web;

internal interface IPublicContentApiClient
{
    Task<PublicContentDto[]> GetContent(CancellationToken ct);

    Task<PublicContentDto?> GetContent(string key, CancellationToken ct);

    Task<PublicContentDto> SaveContent(string key, UpsertPublicContentRequest request, CancellationToken ct);
}
