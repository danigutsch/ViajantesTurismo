namespace ViajantesTurismo.Admin.IntegrationTests.Documents;

/// <summary>Expected document API routes and helpers to normalize observed routes against them.</summary>
internal static class DocumentApiRouteCases
{
    private const string DocumentId = "00000000-0000-0000-0000-000000000001";
    private const string BookingId = "00000000-0000-0000-0000-000000000002";

    /// <summary>Gets the expected HTTP method and route for each document endpoint, using fixed placeholder ids.</summary>
    public static IReadOnlyList<(string Method, string Route)> Expected
    {
        get;
    } =
    [
        (HttpMethod.Post.Method, $"/api/v1/documents/bookings/{BookingId}/contract-drafts"),
        (HttpMethod.Get.Method, $"/api/v1/documents/{DocumentId}"),
        (HttpMethod.Post.Method, $"/api/v1/documents/{DocumentId}/review"),
        (HttpMethod.Post.Method, $"/api/v1/documents/{DocumentId}/changes-requested"),
        (HttpMethod.Patch.Method, $"/api/v1/documents/{DocumentId}/fields/greeting"),
        (HttpMethod.Post.Method, $"/api/v1/documents/{DocumentId}/approve"),
        (HttpMethod.Post.Method, $"/api/v1/documents/{DocumentId}/finalize"),
        (HttpMethod.Post.Method, $"/api/v1/documents/{DocumentId}/regenerate"),
        (HttpMethod.Post.Method, $"/api/v1/documents/{DocumentId}/void"),
        (HttpMethod.Get.Method, $"/api/v1/documents/{DocumentId}/download"),
    ];

    /// <summary>Gets <see cref="Expected"/> as xunit theory data.</summary>
    public static TheoryData<string, string> All
    {
        get
        {
            var cases = new TheoryData<string, string>();
            foreach (var (method, route) in Expected)
            {
                cases.Add(method, route);
            }

            return cases;
        }
    }

    /// <summary>Replaces the given booking and document ids in <paramref name="route"/> with the fixed placeholder ids.</summary>
    public static string Normalize(string route, Guid bookingId, params Guid[] documentIds)
    {
        var normalized = route.Replace(bookingId.ToString(), BookingId, StringComparison.OrdinalIgnoreCase);
        foreach (var documentId in documentIds)
        {
            normalized = normalized.Replace(documentId.ToString(), DocumentId, StringComparison.OrdinalIgnoreCase);
        }

        return normalized;
    }
}
