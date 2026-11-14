using System.Text.RegularExpressions;

namespace ViajantesTurismo.HostedTests.Admin.System.Shared;

public static partial class ConsistencyTestRegexes
{
    [GeneratedRegex(@"R\$\s?\d[\d.,\u00A0\u202F]*[.,]\d{2}")]
    public static partial Regex BrlPrice();

    [GeneratedRegex(@"\d{2}/\d{2}/\d{4}")]
    public static partial Regex DateFormat();
}
