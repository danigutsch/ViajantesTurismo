using System.Text.Json;
using PandaBear.Testing.Http;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

/// <summary>Checks the Catalog route and HTTP method of a response link.</summary>
internal static class CatalogLinkAssertions
{
    /// <summary>Asserts that the response links to an expected Catalog GET route.</summary>
    internal static void ShouldHaveGetLink(this JsonElement payload, string relation, string href)
    {
        var link = payload.ShouldHaveLink(relation);
        link.GetProperty("href").GetString().ShouldBe(href);
        link.GetProperty("method").GetString().ShouldBe("GET");
    }
}
