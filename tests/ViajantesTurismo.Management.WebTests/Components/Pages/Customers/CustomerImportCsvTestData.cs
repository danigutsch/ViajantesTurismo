namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers;

/// <summary>CSV builders for customer import tests using the canonical import header set.</summary>
internal static class CustomerImportCsvTestData
{
    /// <summary>Comma-joined canonical import field names.</summary>
    public static readonly string AllCanonicalHeaders =
        string.Join(",", CustomerImportHeaderMatcher.Fields.Select(importField => importField.Name));

    /// <summary>One placeholder value per canonical field, comma-joined.</summary>
    public static readonly string AllCanonicalValues =
        string.Join(",", CustomerImportHeaderMatcher.Fields.Select(_ => "v"));

    /// <summary>Builds a header plus one data row where only the email column is set and others are placeholders.</summary>
    public static string BuildCsvWithEmail(string email)
    {
        var values = CustomerImportHeaderMatcher.Fields
            .Select(field => field.Name.Equals(CustomerImportFieldNames.Email, StringComparison.OrdinalIgnoreCase) ? email : "v");

        return AllCanonicalHeaders + "\n" + string.Join(",", values);
    }

    /// <summary>Builds a header plus one data row using the given values by field name, placeholders elsewhere.</summary>
    public static string BuildCsvWithOverrides(IReadOnlyDictionary<string, string> valuesByField)
    {
        return AllCanonicalHeaders + "\n" + BuildCsvRow(valuesByField);
    }

    /// <summary>Builds one data row in canonical column order, using the placeholder for fields not supplied.</summary>
    public static string BuildCsvRow(IReadOnlyDictionary<string, string> valuesByField)
    {
        var values = CustomerImportHeaderMatcher.Fields
            .Select(field => valuesByField.GetValueOrDefault(field.Name, "v"));

        return string.Join(",", values);
    }
}
