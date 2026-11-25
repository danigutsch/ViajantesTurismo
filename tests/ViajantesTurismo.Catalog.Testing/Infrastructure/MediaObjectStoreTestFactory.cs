using SharedKernel.Testing.ObjectStorage;
using ViajantesTurismo.Catalog.Domain;

namespace ViajantesTurismo.Catalog.Testing.Infrastructure;

/// <summary>Creates shared in-memory object stores configured with the Catalog media key limit.</summary>
public static class MediaObjectStoreTestFactory
{
    /// <summary>Public base URI that the created stores use for media objects.</summary>
    public static readonly Uri PublicBaseUri = new("https://cdn.example/");

    /// <summary>Creates an empty store that rejects keys longer than the Catalog media object key limit.</summary>
    /// <param name="wrapReadContent">Optional wrapper applied to the stream that each read hands out.</param>
    /// <returns>A new in-memory object store.</returns>
    public static InMemoryObjectStore Create(Func<string, Stream, Stream>? wrapReadContent = null) => new()
    {
        PublicBaseUri = PublicBaseUri,
        MaxObjectKeyLength = CatalogDomainLimits.MaxMediaObjectKeyLength,
        WrapReadContent = wrapReadContent
    };
}
