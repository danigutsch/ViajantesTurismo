using System.Text.RegularExpressions;

namespace ViajantesTurismo.HostedTests.Admin.System.Customers;

public static partial class CustomerImportTestRegexes
{
    [GeneratedRegex(".*/customers/[0-9a-fA-F-]+$")]
    public static partial Regex CustomerUrl();
}
