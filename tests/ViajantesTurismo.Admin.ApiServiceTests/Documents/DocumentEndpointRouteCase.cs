namespace ViajantesTurismo.Admin.ApiServiceTests.Documents;

internal static class DocumentEndpointRouteCase
{
    private const string DocumentId = "00000000-0000-0000-0000-000000000001";
    private const string BookingId = "00000000-0000-0000-0000-000000000002";

    public static IReadOnlyList<(string Method, string Pattern, string RequestPath)> All
    {
        get;
    } =
    [
        (HttpMethod.Post.Method, "api/v1/documents/bookings/{bookingId:guid}/contract-drafts", $"/api/v1/documents/bookings/{BookingId}/contract-drafts"),
        (HttpMethod.Get.Method, "api/v1/documents/{id:guid}", $"/api/v1/documents/{DocumentId}"),
        (HttpMethod.Post.Method, "api/v1/documents/{id:guid}/review", $"/api/v1/documents/{DocumentId}/review"),
        (HttpMethod.Post.Method, "api/v1/documents/{id:guid}/changes-requested", $"/api/v1/documents/{DocumentId}/changes-requested"),
        (HttpMethod.Patch.Method, "api/v1/documents/{id:guid}/fields/{fieldId}", $"/api/v1/documents/{DocumentId}/fields/greeting"),
        (HttpMethod.Post.Method, "api/v1/documents/{id:guid}/approve", $"/api/v1/documents/{DocumentId}/approve"),
        (HttpMethod.Post.Method, "api/v1/documents/{id:guid}/finalize", $"/api/v1/documents/{DocumentId}/finalize"),
        (HttpMethod.Post.Method, "api/v1/documents/{id:guid}/regenerate", $"/api/v1/documents/{DocumentId}/regenerate"),
        (HttpMethod.Post.Method, "api/v1/documents/{id:guid}/void", $"/api/v1/documents/{DocumentId}/void"),
        (HttpMethod.Get.Method, "api/v1/documents/{id:guid}/download", $"/api/v1/documents/{DocumentId}/download"),
        (HttpMethod.Get.Method, "api/v1/documents/bookings/{bookingId:guid}/payment-instructions", $"/api/v1/documents/bookings/{BookingId}/payment-instructions"),
        (HttpMethod.Post.Method, "api/v1/documents/bookings/{bookingId:guid}/payment-instructions", $"/api/v1/documents/bookings/{BookingId}/payment-instructions"),
        (HttpMethod.Post.Method, "api/v1/documents/bookings/{bookingId:guid}/payment-instructions/{id:guid}/regenerate", $"/api/v1/documents/bookings/{BookingId}/payment-instructions/{DocumentId}/regenerate"),
        (HttpMethod.Get.Method, "api/v1/documents/{id:guid}/payment-instructions", $"/api/v1/documents/{DocumentId}/payment-instructions"),
        (HttpMethod.Post.Method, "api/v1/documents/{id:guid}/payment-instructions/approve", $"/api/v1/documents/{DocumentId}/payment-instructions/approve"),
        (HttpMethod.Post.Method, "api/v1/documents/{id:guid}/payment-instructions/finalize", $"/api/v1/documents/{DocumentId}/payment-instructions/finalize"),
        (HttpMethod.Post.Method, "api/v1/documents/{id:guid}/payment-instructions/withdraw-approval", $"/api/v1/documents/{DocumentId}/payment-instructions/withdraw-approval"),
        (HttpMethod.Post.Method, "api/v1/documents/{id:guid}/payment-instructions/void", $"/api/v1/documents/{DocumentId}/payment-instructions/void"),
        (HttpMethod.Post.Method, "api/v1/documents/{id:guid}/payment-instructions/review", $"/api/v1/documents/{DocumentId}/payment-instructions/review"),
    ];
}
