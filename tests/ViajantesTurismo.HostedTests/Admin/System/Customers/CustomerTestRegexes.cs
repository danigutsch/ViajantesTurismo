using System.Text.RegularExpressions;

namespace ViajantesTurismo.HostedTests.Admin.System.Customers;

public static partial class CustomerTestRegexes
{
    [GeneratedRegex("/customers/create/contact$")]
    public static partial Regex ContactStep();

    [GeneratedRegex("/customers/create/physical$")]
    public static partial Regex PhysicalStep();

    [GeneratedRegex("/customers/create/medical$")]
    public static partial Regex MedicalStep();
}
