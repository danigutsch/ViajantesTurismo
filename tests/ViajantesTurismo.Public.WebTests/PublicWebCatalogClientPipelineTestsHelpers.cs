namespace ViajantesTurismo.Public.WebTests;

/// <summary>Helpers for inspecting the outbound HttpClient handler pipeline of Public Web.</summary>
internal static class PublicWebCatalogClientPipelineTestsHelpers
{
    /// <summary>Lists the full type names of the delegating handlers in a pipeline, outermost first.</summary>
    public static List<string> HandlerTypeNames(HttpMessageHandler handler)
    {
        var names = new List<string>();
        for (var current = handler as DelegatingHandler; current is not null; current = current.InnerHandler as DelegatingHandler)
        {
            names.Add(current.GetType().FullName!);
        }

        return names;
    }
}
