namespace ViajantesTurismo.Admin.ApiService;

/// <summary>
/// Defines Admin API permission policies and provider-role mappings.
/// </summary>
internal static class AdminAuthorization
{
    /// <summary>Permission to read bookings.</summary>
    public const string BookingRead = "booking.read";
    /// <summary>Permission to create and modify bookings.</summary>
    public const string BookingWrite = "booking.write";
    /// <summary>Permission to delete bookings.</summary>
    public const string BookingDelete = "booking.delete";
    /// <summary>Permission to import customers from CSV.</summary>
    public const string CustomerImport = "customer.import";
    /// <summary>Permission to read customers with sensitive fields masked.</summary>
    public const string CustomerRead = "customer.read";
    /// <summary>Permission to read sensitive customer fields.</summary>
    public const string CustomerSensitiveRead = "customer.sensitive.read";
    /// <summary>Permission to create and modify customers.</summary>
    public const string CustomerWrite = "customer.write";
    /// <summary>Permission to manage generated documents.</summary>
    public const string DocumentManage = "document.manage";
    /// <summary>Permission to read generated error documentation.</summary>
    public const string DocumentationRead = "documentation.read";
    /// <summary>Permission to manage payment accounts, schedules and instructions, including sensitive payment data.</summary>
    public const string PaymentManage = "payment.manage";
    /// <summary>Permission to read masked payment data.</summary>
    public const string PaymentRead = "payment.read";
    /// <summary>Permission to record payment-related changes.</summary>
    public const string PaymentWrite = "payment.write";
    /// <summary>Permission to read tours.</summary>
    public const string TourRead = "tour.read";
    /// <summary>Permission to create and modify tours.</summary>
    public const string TourWrite = "tour.write";

    /// <summary>Maps each provider role name to the permissions it grants.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyCollection<string>> PermissionsByRole
    {
        get;
    } =
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
                PaymentManage,
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
            ],
            ["Reader"] =
            [
                BookingRead,
                CustomerRead,
                CustomerSensitiveRead,
                PaymentRead,
                TourRead
            ]
        };
}
