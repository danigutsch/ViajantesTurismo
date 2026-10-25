namespace ViajantesTurismo.Admin.IntegrationTests.Documents;

internal static class DocumentApiRouteCases
{
    private const string DocumentId = "00000000-0000-0000-0000-000000000001";
    private const string BookingId = "00000000-0000-0000-0000-000000000002";

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
