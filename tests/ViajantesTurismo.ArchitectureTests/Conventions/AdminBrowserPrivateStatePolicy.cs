using System.Text.Json;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static ViajantesTurismo.ArchitectureTests.Conventions.AdminTestArchitectureGuardTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Guards the app-owned private fixture operations from browser test bodies.</summary>
internal static class AdminBrowserPrivateStatePolicy
{
    private static readonly HashSet<string> PrivateOperations = new(StringComparer.Ordinal)
    {
        "CountCatalogTourEvents",
        "GetRejectedDocumentReadAuditActor",
        "RequeueCatalogTransportMessageForAdminTour",
        "ResetToKnownBaseline",
        "WaitForCatalogTransportMessageProcessed",
    };

    /// <summary>Reports private fixture access and reviewed exceptions that no longer match a call.</summary>
    internal static IEnumerable<string> FindViolations(string systemRoot, string exceptionsFile)
    {
        var approved = ReadExceptions(exceptionsFile);
        foreach (var sourceFile in Directory.EnumerateFiles(systemRoot, "*Tests.cs", SearchOption.AllDirectories))
        {
            if (IsGeneratedTestPath(sourceFile))
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(systemRoot, sourceFile).Replace('\\', '/');
            var syntax = CSharpSyntaxTree.ParseText(File.ReadAllText(sourceFile)).GetRoot();
            var browserTests = syntax.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .Where(AdminSystemFixturePolicy.UsesBrowserFixture);
            foreach (var testClass in browserTests)
            {
                foreach (var test in testClass.Members.OfType<MethodDeclarationSyntax>())
                {
                    foreach (var operation in test.DescendantNodes().OfType<SimpleNameSyntax>().Where(IsPrivateAccess))
                    {
                        var access = (relativePath, testClass.Identifier.ValueText, test.Identifier.ValueText, operation.Identifier.ValueText);
                        if (!approved.Remove(access))
                        {
                            var line = operation.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                            yield return $"{relativePath}:{line} {access.Item3} accesses {access.Item4}. "
                                + "Use a browser-visible assertion or move the private assertion to its owning test seam.";
                        }
                    }
                }
            }
        }

        foreach (var (entry, review) in approved)
        {
            yield return $"Remove the unused browser state exception for {entry.File}:{entry.Test} ({entry.Operation}, {review}).";
        }
    }

    private static bool IsPrivateAccess(SimpleNameSyntax name) =>
        name.Parent is MemberAccessExpressionSyntax or MemberBindingExpressionSyntax
        && PrivateOperations.Contains(name.Identifier.ValueText);

    private static Dictionary<(string File, string Class, string Test, string Operation), string> ReadExceptions(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.EnumerateArray().ToDictionary(
            entry => (Required(entry, "file"), Required(entry, "class"), Required(entry, "test"), Required(entry, "operation")),
            entry =>
            {
                _ = Required(entry, "reason");
                return Required(entry, "review");
            });
    }

    private static string Required(JsonElement entry, string property)
    {
        var value = entry.GetProperty(property).GetString();
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"Browser state exception '{property}' must contain text.")
            : value;
    }
}
