namespace ViajantesTurismo.Catalog.ContractTests.ApiClients;

/// <summary>Test handler plumbing that forces request content to be read, as a real transport would.</summary>
internal static class RequestContentReader
{
    /// <summary>Reads the request content, so a failing content stream throws, and otherwise returns 201 Created.</summary>
    /// <param name="request">The request whose content is read.</param>
    /// <param name="ct">The token that cancels the read.</param>
    /// <returns>A 201 Created response when the content was read.</returns>
    public static HttpResponseMessage ReadContentAndRespondCreated(HttpRequestMessage request, CancellationToken ct)
    {
        request.Content.ShouldNotBeNull().ReadAsByteArrayAsync(ct).GetAwaiter().GetResult();
        return new HttpResponseMessage(System.Net.HttpStatusCode.Created);
    }
}
