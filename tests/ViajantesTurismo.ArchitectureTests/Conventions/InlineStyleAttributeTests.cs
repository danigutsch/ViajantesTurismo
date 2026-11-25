using System.Text.RegularExpressions;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>
/// Guards the Razor UIs whose Content-Security-Policy <c>style-src</c> omits <c>'unsafe-inline'</c>: the browser blocks every
/// inline <c>style</c> attribute there, nonce or not, so the markup must use stylesheet classes instead.
/// </summary>
[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, SharedKernel.Testing.TestTraitValues.SecurityCategory)]
public sealed partial class InlineStyleAttributeTests
{
    private static readonly string[] StrictPolicyHosts =
    [
        "ViajantesTurismo.Management.Web",
        "ViajantesTurismo.Public.Web",
    ];

    [Fact]
    public void Razor_markup_of_strict_policy_hosts_declares_no_inline_style_attribute()
    {
        // Arrange
        var root = RepositoryRoot.Locate("ViajantesTurismo.slnx");
        var markupFiles = StrictPolicyHosts
            .SelectMany(host => Directory.EnumerateFiles(Path.Combine(root, "src", host), "*.*", SearchOption.AllDirectories))
            .Where(path => path.EndsWith(".razor", StringComparison.Ordinal) || path.EndsWith(".cshtml", StringComparison.Ordinal))
            .Where(path => !path.Split(Path.DirectorySeparatorChar).Any(segment => segment is "bin" or "obj"))
            .ToList();

        // Act
        var offenders = markupFiles
            .SelectMany(path => File.ReadLines(path).Select((line, index) => (path, line, number: index + 1)))
            .Where(entry => InlineStyleAttribute().IsMatch(entry.line))
            .Select(entry => $"{Path.GetRelativePath(root, entry.path)}:{entry.number}: {entry.line.Trim()}");
        var report = string.Join(Environment.NewLine, offenders);

        // Assert
        markupFiles.ShouldNotBeEmpty();
        report.ShouldBe(string.Empty);
    }

    [GeneratedRegex("""(?<![\w-])style\s*=\s*["'@]""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex InlineStyleAttribute();
}
