using PandaBear.BuildingBlocks;

namespace ViajantesTurismo.Admin.Api;

/// <summary>Builds origin-relative links from an Admin resource's named endpoints.</summary>
internal sealed class AdminResourceLinks
{
    private readonly HttpContext _context;
    private readonly LinkGenerator _linkGenerator;
    private readonly string _itemEndpoint;
    private readonly HypermediaLink _collection;

    /// <summary>Resolves the collection once for all resources in the response.</summary>
    internal AdminResourceLinks(HttpContext context, LinkGenerator linkGenerator, string itemEndpoint, string collectionEndpoint)
    {
        _context = context;
        _linkGenerator = linkGenerator;
        _itemEndpoint = itemEndpoint;
        _collection = new HypermediaLink("collection", GetPath(collectionEndpoint), HttpMethods.Get);
    }

    /// <summary>Returns the resource address and its navigation links.</summary>
    internal (string Location, IReadOnlyList<HypermediaLink> Links) For(Guid id)
    {
        var location = GetPath(_itemEndpoint, new RouteValueDictionary { ["id"] = id });
        return (location, [new HypermediaLink("self", location, HttpMethods.Get), _collection]);
    }

    private string GetPath(string endpoint, RouteValueDictionary? values = null) =>
        _linkGenerator.GetPathByName(_context, endpoint, values)
        ?? throw new InvalidOperationException($"Could not generate a path for Admin endpoint '{endpoint}'.");
}
