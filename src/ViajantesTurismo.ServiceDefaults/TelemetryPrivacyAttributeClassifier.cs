namespace ViajantesTurismo.ServiceDefaults;

internal static class TelemetryPrivacyAttributeClassifier
{
    private static HashSet<string> SensitiveAttributeNames { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "authorization",
        "booking.id",
        "booking_id",
        "bookingId",
        "commandText",
        "cookie",
        "customer.email",
        "customer.id",
        "customer_email",
        "customer_id",
        "customerEmail",
        "customerId",
        "email",
        "enduser.id",
        "error.message",
        "exception.message",
        "exception.stacktrace",
        "http.request.body",
        "http.response.body",
        "http.target",
        "http.url",
        "http.user_agent",
        "media.object.key",
        "media.object_key",
        "object.key",
        "ObjectKey",
        "parameters",
        "Path",
        "PathBase",
        "payment.id",
        "payment_id",
        "paymentId",
        "QueryString",
        "RequestBody",
        "RequestPath",
        "RequestUri",
        "ResponseBody",
        "set-cookie",
        "Uri",
        "url.full",
        "url.path",
        "url.query",
        "user_agent.original",
        "user.id",
        "aws.s3.copy_source",
        "aws.s3.key",
        "aws.s3.upload_id"
    };

    public static bool IsSensitive(string name)
    {
        return SensitiveAttributeNames.Contains(name)
            || name.StartsWith("db.query.parameter.", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("http.request.header.", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("http.response.header.", StringComparison.OrdinalIgnoreCase);
    }
}
