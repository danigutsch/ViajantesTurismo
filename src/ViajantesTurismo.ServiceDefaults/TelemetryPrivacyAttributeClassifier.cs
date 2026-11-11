namespace ViajantesTurismo.ServiceDefaults;

/// <summary>
/// Classifies the business telemetry attribute names that may carry sensitive data. The SharedKernel privacy baseline
/// (credentials, URLs, query text, storage and messaging identifiers) always applies in addition to this policy.
/// </summary>
internal static class TelemetryPrivacyAttributeClassifier
{
    private static HashSet<string> SensitiveAttributeNames
    {
        get;
    } = new(StringComparer.OrdinalIgnoreCase)
    {
        "booking.id",
        "booking_id",
        "bookingId",
        "customer.email",
        "customer.id",
        "customer_email",
        "customer_id",
        "customerEmail",
        "customerId",
        "KeyId"
    };

    /// <summary>Returns whether the attribute name is a sensitive business attribute that must not be exported.</summary>
    public static bool IsSensitive(string name)
    {
        return name.StartsWith("payment", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("financial", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("recipient", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("fx.quote", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("receipt", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("refund", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("reconciliation", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("allocation", StringComparison.OrdinalIgnoreCase)
            || SensitiveAttributeNames.Contains(name);
    }
}
