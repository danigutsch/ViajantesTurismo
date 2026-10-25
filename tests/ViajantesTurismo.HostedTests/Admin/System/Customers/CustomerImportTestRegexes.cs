using System.Text.RegularExpressions;

namespace ViajantesTurismo.Admin.SystemTests.Customers;

public static partial class CustomerImportTestRegexes
{
    [GeneratedRegex(".*/customers/[0-9a-fA-F-]+$")]
    public static partial Regex CustomerUrl();
}
