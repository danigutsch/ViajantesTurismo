using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

internal static partial class AnalyzerSuppressionPolicyTestsHelpers
{
    private const string ProductionApiHttpDiagnosticId = "VTARCH001";

    private static readonly MetadataReference CoreLibraryReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);

    public static IEnumerable<string> EnumerateRepositoryFiles(string repositoryRoot, string searchPattern)
    {
        return EnumerateRepositoryFiles(new DirectoryInfo(repositoryRoot), searchPattern);
    }

    public static bool IsIgnoredPath(string path)
    {
        return ContainsDirectorySegment(path, "bin")
            || ContainsDirectorySegment(path, "obj")
            || ContainsDirectorySegment(path, ".git")
            || ContainsDirectorySegment(path, ".nuget")
            || path.EndsWith(".feature.cs", StringComparison.Ordinal);
    }

    public static bool IsGeneratedSource(string repositoryRoot, string path)
    {
        var relativePath = Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/');

        return relativePath.Contains("/Migrations/", StringComparison.Ordinal)
            && (relativePath.EndsWith(".Designer.cs", StringComparison.Ordinal)
                || relativePath.EndsWith("ModelSnapshot.cs", StringComparison.Ordinal));
    }

    public static string[] FindNoWarnEntries(string repositoryRoot, string filePath)
    {
        var text = File.ReadAllText(filePath);
        var relativePath = Path.GetRelativePath(repositoryRoot, filePath).Replace('\\', '/');
        var entries = new List<string>();

        foreach (Match match in NoWarnRegex().Matches(text))
        {
            var diagnostics = match.Groups[1].Value
                .Replace("$(NoWarn)", string.Empty, StringComparison.Ordinal)
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => $"{relativePath}:{value}");

            entries.AddRange(diagnostics);
        }

        return [.. entries];
    }

    public static string[] FindUnapprovedPragmaWarningSuppressions(string repositoryRoot, string filePath)
    {
        var relativePath = Path.GetRelativePath(repositoryRoot, filePath).Replace('\\', '/');
        return FindUnapprovedPragmaWarningSuppressionsInSource(File.ReadAllText(filePath), relativePath);
    }

    public static string[] FindUnapprovedPragmaWarningSuppressionsInSource(string source, string displayPath)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var root = syntaxTree.GetRoot();
        var directives = root
            .DescendantTrivia(descendIntoTrivia: true)
            .Select(static trivia => trivia.GetStructure())
            .OfType<PragmaWarningDirectiveTriviaSyntax>()
            .OrderBy(static directive => directive.SpanStart)
            .ToArray();
        var violations = new List<string>();

        var index = 0;
        while (index < directives.Length)
        {
            var disable = directives[index];
            var lineNumber = syntaxTree.GetLineSpan(disable.Span).StartLinePosition.Line + 1;

            if (!IsDirectiveFor(disable, SyntaxKind.DisableKeyword, ProductionApiHttpDiagnosticId))
            {
                violations.Add($"{displayPath}:L{lineNumber} {disable.ToString().Trim()}");
                index++;
                continue;
            }

            if (!HasJustification(syntaxTree, disable))
            {
                violations.Add($"{displayPath}:L{lineNumber} missing '// Justification: ...'");
            }

            if (index + 1 >= directives.Length
                || !IsDirectiveFor(directives[index + 1], SyntaxKind.RestoreKeyword, ProductionApiHttpDiagnosticId))
            {
                violations.Add($"{displayPath}:L{lineNumber} must be followed by '#pragma warning restore {ProductionApiHttpDiagnosticId}'");
                index++;
                continue;
            }

            var restore = directives[index + 1];
            var enclosedStatements = root.DescendantNodes()
                .OfType<StatementSyntax>()
                .Where(statement => statement.SpanStart >= disable.FullSpan.End && statement.Span.End <= restore.FullSpan.Start)
                .Where(statement => !statement.Ancestors()
                    .OfType<StatementSyntax>()
                    .Any(ancestor => ancestor.SpanStart >= disable.FullSpan.End && ancestor.Span.End <= restore.FullSpan.Start))
                .ToArray();

            if (enclosedStatements.Length != 1 || !IsExactSiteStatement(enclosedStatements[0]))
            {
                violations.Add($"{displayPath}:L{lineNumber} must suppress exactly one statement");
            }

            index += 2;
        }

        return [.. violations];
    }

    public static string[] FindBroadProductionHttpSuppressions(string repositoryRoot, string filePath)
    {
        var text = File.ReadAllText(filePath);
        var relativePath = Path.GetRelativePath(repositoryRoot, filePath).Replace('\\', '/');
        return FindBroadProductionHttpSuppressionsInSource(text, relativePath);
    }

    public static string[] FindBroadProductionHttpSuppressionsInSource(string text, string displayPath)
    {
        var violations = FindMsBuildDiagnosticEntries(text, NoWarnRegex())
            .Concat(FindMsBuildDiagnosticEntries(text, WarningsNotAsErrorsRegex()))
            .Where(static entry => string.Equals(entry, ProductionApiHttpDiagnosticId, StringComparison.OrdinalIgnoreCase))
            .Select(entry => $"{displayPath}:{entry}")
            .ToList();
        violations.AddRange(FindForbiddenAnalyzerPolicyInputs(text, displayPath));
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        for (var index = 0; index < lines.Length; index++)
        {
            var normalizedLine = string.Concat(lines[index].Where(static character => !char.IsWhiteSpace(character)));
            var commentIndex = normalizedLine.IndexOfAny(['#', ';']);
            var setting = commentIndex < 0 ? normalizedLine : normalizedLine.Substring(0, commentIndex);
            if (IsBroadAnalyzerSeveritySuppression(setting))
            {
                violations.Add($"{displayPath}:L{index + 1} {lines[index].Trim()}");
            }
        }

        return [.. violations];
    }

    public static string[] FindProductionHttpPragmaSuppressions(string repositoryRoot, string filePath)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(filePath));
        var relativePath = Path.GetRelativePath(repositoryRoot, filePath).Replace('\\', '/');

        return syntaxTree.GetRoot()
            .DescendantTrivia(descendIntoTrivia: true)
            .Select(static trivia => trivia.GetStructure())
            .OfType<PragmaWarningDirectiveTriviaSyntax>()
            .Where(static directive => directive.DisableOrRestoreKeyword.IsKind(SyntaxKind.DisableKeyword)
                && directive.ErrorCodes.Any(static code => string.Equals(code.ToString(), ProductionApiHttpDiagnosticId, StringComparison.Ordinal)))
            .Select(directive => $"{relativePath}:L{syntaxTree.GetLineSpan(directive.Span).StartLinePosition.Line + 1}")
            .ToArray();
    }

    public static string[] FindProductionHttpSuppressMessageSuppressions(string repositoryRoot, string filePath)
    {
        var relativePath = Path.GetRelativePath(repositoryRoot, filePath).Replace('\\', '/');
        return FindProductionHttpSuppressMessageSuppressionsInSource(File.ReadAllText(filePath), relativePath);
    }

    public static string[] FindProductionHttpSuppressMessageSuppressionsInSource(string source, string displayPath)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var root = syntaxTree.GetRoot();
        var usingDirectives = root.DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .ToArray();
        var suppressionAliases = usingDirectives
            .Select(static directive => directive.Alias is not null
                && directive.Name is not null
                && directive.Name.ToString().EndsWith("SuppressMessageAttribute", StringComparison.Ordinal)
                    ? directive.Alias.Name.Identifier.ValueText
                    : null)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);
        var violations = usingDirectives
            .Where(static directive => directive.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword)
                && directive.Alias is not null
                && directive.Name is not null
                && directive.Name.ToString().EndsWith("SuppressMessageAttribute", StringComparison.Ordinal))
            .Select(directive => $"{displayPath}:L{syntaxTree.GetLineSpan(directive.Span).StartLinePosition.Line + 1}")
            .ToList();
        var suppressionAttributes = root
            .DescendantNodes()
            .OfType<AttributeSyntax>()
            .Where(attribute => IsSuppressMessageAttribute(attribute, suppressionAliases))
            .ToArray();

        if (suppressionAttributes.Length == 0)
        {
            return [.. violations];
        }

        var semanticModel = CSharpCompilation.Create(
            "AnalyzerSuppressionPolicy",
            [syntaxTree],
            [CoreLibraryReference]).GetSemanticModel(syntaxTree);

        violations.AddRange(suppressionAttributes
            .Where(attribute => attribute.ArgumentList?.Arguments.Any(argument => IsProductionHttpDiagnostic(argument, semanticModel)) == true)
            .Select(attribute => $"{displayPath}:L{syntaxTree.GetLineSpan(attribute.Span).StartLinePosition.Line + 1}"));

        return [.. violations];
    }

    public static bool EnablesProductionHttpAnalyzer(string filePath)
    {
        return EnablesProductionHttpAnalyzer(XDocument.Load(filePath));
    }

    public static bool EnablesProductionHttpAnalyzerInProject(string projectXml)
    {
        ArgumentNullException.ThrowIfNull(projectXml);
        return EnablesProductionHttpAnalyzer(XDocument.Parse(projectXml));
    }

    public static string GetRepositoryRoot()
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

        while (currentDirectory is not null)
        {
            var solutionPath = Path.Combine(currentDirectory.FullName, "ViajantesTurismo.slnx");
            if (File.Exists(solutionPath))
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root from the test output directory.");
    }

    [GeneratedRegex(@"<NoWarn(?:\s+[^>]*)?>\s*([^<]+)</NoWarn>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    public static partial Regex NoWarnRegex();

    [GeneratedRegex(@"<WarningsNotAsErrors(?:\s+[^>]*)?>\s*([^<]+)</WarningsNotAsErrors>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    public static partial Regex WarningsNotAsErrorsRegex();

    [GeneratedRegex(
        @"^\s*\[\s*(?:assembly:\s*)?(?:(?:global::)?System\.Diagnostics\.CodeAnalysis\.)?SuppressMessage(?:Attribute)?\s*\(",
        RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    public static partial Regex SuppressMessageAttributeRegex();

    private static bool EnablesProductionHttpAnalyzer(XDocument project)
    {
        return project.Descendants().Any(static element =>
        {
            var itemPath = (element.Attribute("Include")?.Value ?? element.Attribute("Update")?.Value)?.Replace('\\', '/');

            if (string.Equals(element.Name.LocalName, "ProjectReference", StringComparison.OrdinalIgnoreCase))
            {
                var outputItemType = element.Attribute("OutputItemType")?.Value
                    ?? element.Elements().FirstOrDefault(static child => string.Equals(child.Name.LocalName, "OutputItemType", StringComparison.OrdinalIgnoreCase))?.Value;
                return itemPath?.EndsWith("/ViajantesTurismo.Architecture.Analyzers.csproj", StringComparison.OrdinalIgnoreCase) == true
                    && string.Equals(outputItemType, "Analyzer", StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(element.Name.LocalName, "Analyzer", StringComparison.OrdinalIgnoreCase)
                && itemPath?.Contains("ViajantesTurismo.Architecture.Analyzers", StringComparison.OrdinalIgnoreCase) == true;
        });
    }

    private static IEnumerable<string> EnumerateRepositoryFiles(DirectoryInfo directory, string searchPattern)
    {
        foreach (var file in directory.EnumerateFiles(searchPattern))
        {
            yield return file.FullName;
        }

        foreach (var childDirectory in directory.EnumerateDirectories())
        {
            if (IsIgnoredDirectoryName(childDirectory.Name))
            {
                continue;
            }

            foreach (var file in EnumerateRepositoryFiles(childDirectory, searchPattern))
            {
                yield return file;
            }
        }
    }

    private static bool IsIgnoredDirectoryName(string directoryName)
    {
        return directoryName is "bin" or "obj" or ".git" or ".nuget" or ".worktrees";
    }

    private static bool ContainsDirectorySegment(string path, string directoryName)
    {
        return path.Contains(
            $"{Path.DirectorySeparatorChar}{directoryName}{Path.DirectorySeparatorChar}",
            StringComparison.Ordinal);
    }

    private static bool IsDirectiveFor(PragmaWarningDirectiveTriviaSyntax directive, SyntaxKind keyword, string diagnosticId)
    {
        return directive.DisableOrRestoreKeyword.IsKind(keyword)
            && directive.ErrorCodes.Count == 1
            && string.Equals(directive.ErrorCodes[0].ToString(), diagnosticId, StringComparison.Ordinal);
    }

    private static bool IsExactSiteStatement(StatementSyntax statement)
    {
        var descendantNodes = statement.DescendantNodes().ToArray();
        if (descendantNodes.OfType<StatementSyntax>().Any()
            || descendantNodes.Any(static node => node is ConditionalExpressionSyntax
                or SwitchExpressionSyntax
                or ConditionalAccessExpressionSyntax
                || node.IsKind(SyntaxKind.CoalesceExpression)
                || node.IsKind(SyntaxKind.LogicalAndExpression)
                || node.IsKind(SyntaxKind.LogicalOrExpression)))
        {
            return false;
        }

        var invocationSites = descendantNodes.OfType<InvocationExpressionSyntax>().Count();
        var objectCreationSites = descendantNodes.Count(static node => (node is ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax)
            && !node.Ancestors().OfType<InvocationExpressionSyntax>().Any());
        if (invocationSites + objectCreationSites != 1)
        {
            return false;
        }

        return statement switch
        {
            ExpressionStatementSyntax => true,
            LocalDeclarationStatementSyntax localDeclaration => localDeclaration.Declaration.Variables.Count == 1,
            ReturnStatementSyntax => true,
            _ => false
        };
    }

    private static bool IsSuppressMessageAttribute(AttributeSyntax attribute, HashSet<string> suppressionAliases)
    {
        var attributeName = attribute.Name switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
            AliasQualifiedNameSyntax aliasQualified => aliasQualified.Name.Identifier.ValueText,
            _ => attribute.Name.ToString()
        };

        return attributeName is "SuppressMessage"
            or "SuppressMessageAttribute"
            or "UnconditionalSuppressMessage"
            or "UnconditionalSuppressMessageAttribute"
            || suppressionAliases.Contains(attributeName);
    }

    private static bool IsProductionHttpDiagnostic(AttributeArgumentSyntax argument, SemanticModel semanticModel)
    {
        var constantValue = semanticModel.GetConstantValue(argument.Expression);
        if (constantValue is { HasValue: true, Value: string value }
            && (string.Equals(value, ProductionApiHttpDiagnosticId, StringComparison.OrdinalIgnoreCase)
                || value.StartsWith(ProductionApiHttpDiagnosticId + ":", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return argument.Expression.ToString().Contains("ProductionApiHttp", StringComparison.Ordinal);
    }

    private static bool IsBroadAnalyzerSeveritySuppression(string setting)
    {
        var separatorIndex = setting.IndexOf('=', StringComparison.Ordinal);
        if (separatorIndex < 0)
        {
            return false;
        }

        var key = setting.Substring(0, separatorIndex);
        var severity = setting.Substring(separatorIndex + 1);
        var suppressesDiagnostics = string.Equals(severity, "none", StringComparison.OrdinalIgnoreCase)
            || string.Equals(severity, "silent", StringComparison.OrdinalIgnoreCase)
            || string.Equals(severity, "suggestion", StringComparison.OrdinalIgnoreCase);

        return suppressesDiagnostics
            && (string.Equals(key, $"dotnet_diagnostic.{ProductionApiHttpDiagnosticId}.severity", StringComparison.OrdinalIgnoreCase)
                || string.Equals(key, "dotnet_analyzer_diagnostic.category-Architecture.severity", StringComparison.OrdinalIgnoreCase)
                || string.Equals(key, "dotnet_analyzer_diagnostic.severity", StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasJustification(SyntaxTree syntaxTree, PragmaWarningDirectiveTriviaSyntax directive)
    {
        const string justificationMarker = "// Justification:";
        var sourceLine = syntaxTree.GetText().Lines.GetLineFromPosition(directive.SpanStart).ToString();
        var markerIndex = sourceLine.IndexOf(justificationMarker, StringComparison.Ordinal);

        return markerIndex >= 0
            && sourceLine[(markerIndex + justificationMarker.Length)..].Trim().Length >= 12;
    }

    private static IEnumerable<string> FindMsBuildDiagnosticEntries(string text, Regex regex)
    {
        return regex.Matches(text)
            .SelectMany(static match => match.Groups[1].Value
                .Replace("$(NoWarn)", string.Empty, StringComparison.Ordinal)
                .Replace("$(WarningsNotAsErrors)", string.Empty, StringComparison.Ordinal)
                .Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static IEnumerable<string> FindForbiddenAnalyzerPolicyInputs(string text, string displayPath)
    {
        if (!text.TrimStart().StartsWith('<'))
        {
            return [];
        }

        var project = XDocument.Parse(text);
        return project.Descendants()
            .Where(static element =>
                (string.Equals(element.Name.LocalName, "GlobalAnalyzerConfigFiles", StringComparison.OrdinalIgnoreCase)
                    && element.Attributes().Any(static attribute =>
                        (string.Equals(attribute.Name.LocalName, "Include", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(attribute.Name.LocalName, "Update", StringComparison.OrdinalIgnoreCase))
                        && !string.IsNullOrWhiteSpace(attribute.Value)))
                || (string.Equals(element.Name.LocalName, "CodeAnalysisRuleSet", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(element.Value)))
            .Select(element => $"{displayPath}:{element.Name.LocalName}");
    }
}
