namespace ViajantesTurismo.Admin.ApiService;

/// <summary>
/// Defines Admin API permission policies and provider-role mappings.
/// </summary>
internal static class AdminAuthorization
{
    public const string BookingRead = "booking.read";
    public const string BookingWrite = "booking.write";
    public const string BookingDelete = "booking.delete";
    public const string CustomerImport = "customer.import";
    public const string CustomerRead = "customer.read";
    public const string CustomerSensitiveRead = "customer.sensitive.read";
    public const string CustomerWrite = "customer.write";
    public const string DocumentManage = "document.manage";
    public const string DocumentationRead = "documentation.read";
    public const string PaymentRead = "payment.read";
    public const string PaymentWrite = "payment.write";
    public const string TourRead = "tour.read";
    public const string TourWrite = "tour.write";

    public static IReadOnlyDictionary<string, IReadOnlyCollection<string>> PermissionsByRole { get; } =
        new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.Ordinal)
        {
            ["Admin"] =
            [
                BookingRead,
                BookingWrite,
                BookingDelete,
                CustomerImport,
                CustomerRead,
                CustomerSensitiveRead,
                CustomerWrite,
                DocumentManage,
                DocumentationRead,
                PaymentRead,
                PaymentWrite,
                TourRead,
                TourWrite
            ],
            ["Operator"] =
            [
                BookingRead,
                BookingWrite,
                DocumentationRead,
                PaymentRead,
                PaymentWrite,
                TourRead,
                TourWrite
            ]
        };
}
