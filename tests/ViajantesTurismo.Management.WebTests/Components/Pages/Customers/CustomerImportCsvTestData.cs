namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers;

internal static class CustomerImportCsvTestData
{
    public static string AllCanonicalHeaders =>
        string.Join(",", CustomerImportHeaderMatcher.Fields.Select(importField => importField.Name));

    public static string BuildCsvWithEmail(string email)
    {
        var values = CustomerImportHeaderMatcher.Fields
            .Select(field => field.Name.Equals("Email", StringComparison.OrdinalIgnoreCase) ? email : "v");

        return AllCanonicalHeaders + "\n" + string.Join(",", values);
    }

    public static string BuildCsvWithOverrides(IReadOnlyDictionary<string, string> valuesByField)
    {
        return AllCanonicalHeaders + "\n" + BuildCsvRow(valuesByField);
    }

    public static string BuildCsvRow(IReadOnlyDictionary<string, string> valuesByField)
    {
        var values = CustomerImportHeaderMatcher.Fields
            .Select(field => valuesByField.GetValueOrDefault(field.Name, "v"));

        return string.Join(",", values);
    }
}
