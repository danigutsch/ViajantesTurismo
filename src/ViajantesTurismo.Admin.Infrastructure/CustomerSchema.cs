namespace ViajantesTurismo.Admin.Infrastructure;

/// <summary>Database schema names and unique rules for customers.</summary>
internal static class CustomerSchema
{
    /// <summary>Name of the unique index on customer email.</summary>
    public const string EmailUniqueIndex = "UX_CustomerContactInfo_Email";

    /// <summary>The unique rule of the customer email index, reported when two customers would share an email.</summary>
    public const string EmailUniqueRule = "customer.email";
}
