using System.Text.RegularExpressions;

namespace ViajantesTurismo.Admin.SystemTests.Shared;

public static partial class ConsistencyTestRegexes
{
    [GeneratedRegex(@"R\$\s[\d,]+\.\d{2}")]
    public static partial Regex BrlPrice();

    [GeneratedRegex(@"\d{2}/\d{2}/\d{4}")]
    public static partial Regex DateFormat();
}
