using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharedKernel.Testing;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Checks the fixture dependency used by Admin system tests.</summary>
internal static class AdminSystemFixturePolicy
{
    private const string SharedFixture = "AspireSystemTestFixture";
    private const string BrowserBase = "AspireSystemTestBase";

    /// <summary>Reports whether the source declares a system test without the shared fixture.</summary>
    internal static bool HasMissingFixture(string source) =>
        CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Any(testClass => DeclaresSystemScope(testClass) && !UsesSharedFixture(testClass));

    /// <summary>Reports whether the class uses the Admin Playwright System fixture.</summary>
    internal static bool UsesBrowserFixture(ClassDeclarationSyntax testClass) =>
        testClass.BaseList?.Types.Any(baseType =>
            UnqualifiedName(baseType.Type)?.Identifier.ValueText is BrowserBase or "BrowserTest" or "TracedPageTest") == true;

    private static bool DeclaresSystemScope(ClassDeclarationSyntax testClass) =>
        HasSystemScope(testClass.AttributeLists)
        || testClass.Members.OfType<MethodDeclarationSyntax>().Any(method => HasSystemScope(method.AttributeLists));

    private static bool HasSystemScope(SyntaxList<AttributeListSyntax> lists) =>
        lists.SelectMany(list => list.Attributes).Any(attribute =>
            attribute.Name.GetLastToken().ValueText is nameof(TestScope) or nameof(TestScopeAttribute)
            && attribute.ArgumentList is { Arguments: [{ Expression: MemberAccessExpressionSyntax scope }] }
            && scope.Expression.GetLastToken().ValueText == nameof(TestScope)
            && scope.Name.Identifier.ValueText == nameof(TestScope.System));

    private static bool UsesSharedFixture(ClassDeclarationSyntax testClass) =>
        testClass.BaseList?.Types.Any(baseType =>
            UnqualifiedName(baseType.Type) is GenericNameSyntax { TypeArgumentList.Arguments: [var fixture] } generic
            && generic.Identifier.ValueText is "IClassFixture" or BrowserBase
            && fixture.GetLastToken().ValueText == SharedFixture) == true;

    private static SimpleNameSyntax? UnqualifiedName(TypeSyntax type) => type switch
    {
        SimpleNameSyntax name => name,
        QualifiedNameSyntax qualified => qualified.Right,
        AliasQualifiedNameSyntax aliasQualified => aliasQualified.Name,
        _ => null,
    };
}
