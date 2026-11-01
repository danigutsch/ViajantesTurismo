namespace ViajantesTurismo.Catalog.ApiServiceTests;

/// <summary>
/// Case-insensitive property-name fragments for the admin-only field groups in docs/privacy-classification.md.
/// </summary>
internal static class AdminOnlyPiiFieldNames
{
    /// <summary>Customer identity and contact (Personal).</summary>
    public static readonly IReadOnlyList<string> CustomerIdentityAndContact =
        ["email", "phone", "address", "nationality", "occupation", "emergency", "social", "customer"];

    /// <summary>Customer high-risk details (Sensitive).</summary>
    public static readonly IReadOnlyList<string> CustomerHighRiskDetails =
        ["nationalid", "birthdate", "health", "allergy", "medical", "accommodation", "physical"];

    /// <summary>Booking relationship and free text (Personal or Sensitive).</summary>
    public static readonly IReadOnlyList<string> BookingRelationship =
        ["booking", "traveller", "traveler", "notes", "discount"];

    /// <summary>Payment and pricing (Financial).</summary>
    public static readonly IReadOnlyList<string> Payment = ["payment"];

    /// <summary>Authentication material (Credential).</summary>
    public static readonly IReadOnlyList<string> AuthenticationMaterial =
        ["password", "token", "secret", "apikey", "cookie"];

    /// <summary>Every classified admin-only fragment.</summary>
    public static IEnumerable<string> All =>
        CustomerIdentityAndContact
            .Concat(CustomerHighRiskDetails)
            .Concat(BookingRelationship)
            .Concat(Payment)
            .Concat(AuthenticationMaterial);

    /// <summary>Returns the property names that contain any classified admin-only fragment.</summary>
    /// <param name="propertyNames">The JSON property names of a public payload.</param>
    public static IReadOnlyList<string> Matching(IEnumerable<string> propertyNames)
    {
        ArgumentNullException.ThrowIfNull(propertyNames);

        return [.. propertyNames.Where(name => All.Any(fragment => name.Contains(fragment, StringComparison.OrdinalIgnoreCase)))];
    }
}
