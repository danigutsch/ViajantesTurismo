namespace ViajantesTurismo.Catalog.Contracts;

/// <summary>
/// HTTP client contract for public content management endpoints.
/// </summary>
public interface IPublicContentApiClient
{
    /// <summary>
    /// Gets all public content entries.
    /// </summary>
    Task<PublicContentDto[]> GetContent(CancellationToken ct);

    /// <summary>
    /// Gets a public content entry by key.
    /// </summary>
    Task<PublicContentDto?> GetContent(string key, CancellationToken ct);

    /// <summary>
    /// Saves a public content entry.
    /// </summary>
    Task<PublicContentDto> SaveContent(string key, UpsertPublicContentRequest request, CancellationToken ct);
}
