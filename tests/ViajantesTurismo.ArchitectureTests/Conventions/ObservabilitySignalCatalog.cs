using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PandaBear.Observability;
using static ViajantesTurismo.ArchitectureTests.Conventions.ObservabilityAssetsTestsHelpers;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Discovers application signal names and explicit registrations for the catalog drift checks.</summary>
internal static class ObservabilitySignalCatalog
{
    private static readonly Lazy<CSharpCompilation> Source = new(CreateCompilation);

    /// <summary>Sources owned by the pinned SharedKernel libraries that the application uses.</summary>
    public static readonly string[] SharedKernelActivitySources =
    [
        PandaBear.Mediator.SharedKernelMediatorActivitySource.ActivitySourceName,
        PandaBear.HttpClients.HttpOutcomeMapper.ActivitySourceName,
        PandaBear.EventSourcing.Npgsql.PostgreSqlEventSourcingTelemetry.Name,
        "SharedKernel.MalwareScanning",
        PandaBear.ObjectStorage.S3.S3ObjectStoreTelemetry.Name,
        PandaBear.Scheduling.SchedulingTelemetry.Name,
    ];

    /// <summary>Stable events owned by the pinned SharedKernel libraries and documented here.</summary>
    public static readonly string[] SharedKernelLogEvents =
    [
        $"PandaBear.AspNetCore/{PandaBear.AspNetCore.AspNetCoreTelemetry.UnhandledExceptionEventId}",
        $"PandaBear.AspNetCore/{PandaBear.AspNetCore.AspNetCoreTelemetry.AuthenticationEventId}",
        $"PandaBear.AspNetCore/{PandaBear.AspNetCore.AspNetCoreTelemetry.RateLimitRejectionEventId}",
        "PandaBear.Blazor/7001",
        "PermanentRedirectExtensions/1",
        "PollingBackgroundService/1",
        "ApplicationVersionLogger/1",
        "ResultTransactionBehavior/1",
        "PostgreSqlIntegrationEventTransportConsumer/1",
        "PostgreSqlIntegrationEventTransportConsumer/2",
    ];

    /// <summary>Gets keys in the first cell of a named catalog table.</summary>
    public static string[] CatalogKeys(string catalog, string heading) =>
        [.. catalog.Split('\n').SkipWhile(line => line.TrimEnd() != $"## {heading}").Skip(1)
            .TakeWhile(line => !line.StartsWith("## ", StringComparison.Ordinal))
            .SelectMany(line => CatalogKey().Matches(line).Select(match => match.Groups["key"].Value))];

    /// <summary>Gets explicit names and wildcard subscriptions from the application and SharedKernel.</summary>
    public static string[] RegisteredNames(string method) =>
        [.. Nodes<InvocationExpressionSyntax>(method)
            .Where(invocation => invocation.Expression is MemberAccessExpressionSyntax member && member.Name.Identifier.ValueText == method)
            .SelectMany(invocation => invocation.ArgumentList.Arguments)
            .Select(argument => SourceName(argument.Expression))
            .Concat(InstrumentationExtensions.NamePatterns).Distinct(StringComparer.Ordinal)];

    /// <summary>Gets whether the registration subscribes to the given meter or activity source.</summary>
    public static bool MatchesRegistration(string pattern, string name) =>
        pattern.EndsWith('*', StringComparison.Ordinal) ? name.StartsWith(pattern[..^1], StringComparison.Ordinal) : name == pattern;

    /// <summary>Gets source names declared by the application, including those registered by wildcard.</summary>
    public static IEnumerable<string> ApplicationActivitySources() => DeclaredNames(nameof(ActivitySource));

    /// <summary>Gets meter names constructed directly or obtained through the application meter factory.</summary>
    public static IEnumerable<string> ApplicationMeters() =>
        DeclaredNames(nameof(Meter)).Concat(Nodes<InvocationExpressionSyntax>(nameof(IMeterFactory))
            .Where(invocation => invocation.Expression is MemberAccessExpressionSyntax member
                && member.Name.Identifier.ValueText == "Create"
                && Model(member.Expression).GetTypeInfo(member.Expression).Type?.Name == nameof(IMeterFactory))
            .Select(invocation => SourceName(invocation.ArgumentList.Arguments[0].Expression)));

    /// <summary>Gets each explicit source-generated log event by its declaring type and numeric ID.</summary>
    public static IEnumerable<string> ApplicationLogEvents()
    {
        var attributes = Nodes<AttributeSyntax>("LoggerMessage")
            .Where(attribute => attribute.Name.GetLastToken().ValueText is "LoggerMessage" or "LoggerMessageAttribute");

        foreach (var attribute in attributes)
        {
            if (EventId(attribute) is { } id)
            {
                var owner = attribute.Ancestors().OfType<TypeDeclarationSyntax>().First().Identifier.ValueText;
                yield return $"{owner}/{Constant<int>(id)}";
            }
        }
    }

    private static ExpressionSyntax? EventId(AttributeSyntax attribute)
    {
        var arguments = attribute.ArgumentList?.Arguments ?? default;
        var named = arguments.FirstOrDefault(argument => argument.NameEquals?.Name.Identifier.ValueText == "EventId"
            || argument.NameColon?.Name.Identifier.ValueText == "eventId");
        if (named is not null)
        {
            return named.Expression;
        }

        // The constructor with a positional EventId takes ID, level, and message.
        var constructorArguments = arguments.Where(argument => argument.NameEquals is null).ToArray();
        return constructorArguments.Length == 3 && constructorArguments[0].NameColon is null
            ? constructorArguments[0].Expression
            : null;
    }

    private static IEnumerable<TSyntax> Nodes<TSyntax>(string sourceToken) where TSyntax : CSharpSyntaxNode =>
        Source.Value.SyntaxTrees.Where(tree => tree.GetText().ToString().Contains(sourceToken, StringComparison.Ordinal))
            .SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<TSyntax>());

    private static IEnumerable<string> DeclaredNames(string type) =>
        Nodes<BaseObjectCreationExpressionSyntax>(type)
            .Where(creation => Model(creation).GetTypeInfo(creation).Type?.Name == type)
            .Select(creation => SourceName(creation.ArgumentList?.Arguments[0].Expression
                ?? throw new InvalidOperationException($"{type} has no name at {creation.GetLocation()}")));

    private static SemanticModel Model(SyntaxNode node) => Source.Value.GetSemanticModel(node.SyntaxTree);

    private static string SourceName(ExpressionSyntax expression)
    {
        // DatabaseObservability is an executable rather than a reference of the architecture test project.
        if (expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax owner, Name.Identifier.ValueText: "MeterName" }
            && owner.Identifier.ValueText == "PostgreSqlIndexHealthTelemetry")
        {
            return "SharedKernel.Observability.Npgsql";
        }

        var declaration = Model(expression).GetSymbolInfo(expression).Symbol?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
        return declaration is PropertyDeclarationSyntax { ExpressionBody.Expression: { } value }
            ? Constant<string>(value)
            : Constant<string>(expression);
    }

    private static T Constant<T>(ExpressionSyntax expression) =>
        Model(expression).GetConstantValue(expression).Value is T value
            ? value
            : throw new InvalidOperationException($"Unresolved telemetry name or ID {expression} at {expression.GetLocation()}.");

    private static CSharpCompilation CreateCompilation()
    {
        var files = Directory.EnumerateFiles(Path.Combine(RepositoryRoot.Locate("ViajantesTurismo.slnx"), "src"), "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Split(Path.DirectorySeparatorChar).Any(part => part is "obj" or "bin"));
        var trees = files.Select(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file));
        MetadataReference[] references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ActivitySource).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location) ?? string.Empty, "System.Runtime.dll")),
        ];
        return CSharpCompilation.Create(nameof(ObservabilitySignalCatalog), trees, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
