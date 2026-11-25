using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static ViajantesTurismo.ArchitectureTests.Conventions.AdminTestArchitectureGuardTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Checks test source for access behind the hosted production entrypoint.</summary>
internal static class HostedTestSeamPolicy
{
    private static readonly HashSet<string> PersistenceTypes = new(StringComparer.Ordinal)
    {
        "NpgsqlConnection",
        "NpgsqlDataSourceBuilder",
        "AdminWriteDbContext",
        "CatalogDbContext",
        "EfBrandingSettingsStore",
    };

    /// <summary>Reports hosted test source that reaches directly into persistence from a transport seam.</summary>
    public static string[] FindDirectPersistenceViolations(string hostedRoot)
    {
        string[] roots =
        [
            Path.Combine(hostedRoot, "Admin", "Api"),
            Path.Combine(hostedRoot, "Admin", "System"),
            Path.Combine(hostedRoot, "Catalog", "Media"),
            Path.Combine(hostedRoot, "Management", "Browser"),
        ];
        return [.. roots.SelectMany(root => Directory.EnumerateFiles(root, "*Tests.cs", SearchOption.AllDirectories))
            .Where(path => !IsGeneratedTestPath(path))
            .Where(path => UsesDirectPersistence(File.ReadAllText(path)))
            .Select(path => Path.GetRelativePath(hostedRoot, path).Replace('\\', '/'))];
    }

    /// <summary>Finds direct construction or creation of the hosted persistence adapters.</summary>
    public static bool UsesDirectPersistence(string source) =>
        CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes().Any(IsDirectPersistenceAccess);

    private static bool IsDirectPersistenceAccess(SyntaxNode node) => node switch
    {
        ObjectCreationExpressionSyntax creation => PersistenceTypes.Contains(creation.Type.GetLastToken().ValueText),
        InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } =>
            access.Expression.GetLastToken().ValueText == "NpgsqlDataSource" && access.Name.Identifier.ValueText == "Create",
        _ => false,
    };
}
