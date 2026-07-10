using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

internal static partial class AppHostOrchestrationTestsHelpers
{
    private static readonly string[] CandidatePlatformPackageFragments =
    [
        "Yarp",
        "Ollama",
        "Mailpit",
        "Mongo",
        "Cosmos",
        "Raven",
        "Qdrant",
        "Weaviate",
        "Flagd",
        "OpenFeature",
        "FeatureManagement"
    ];

    private static readonly string[] CandidatePlatformResourceFragments =
    [
        "AddYarp",
        "Yarp",
        "\"yarp\"",
        "ReverseProxy",
        "AddGateway",
        "EdgeGateway",
        "AddOllama",
        "Ollama",
        "\"ollama\"",
        "AddMailPit",
        "AddMailpit",
        "MailPit",
        "Mailpit",
        "\"mailpit\"",
        "AddMongoDB",
        "AddMongo",
        "MongoDB",
        "\"mongo\"",
        "\"mongodb\"",
        "AddAzureCosmosDB",
        "AddAzureCosmosDb",
        "CosmosDB",
        "\"cosmos\"",
        "RavenDB",
        "RavenDb",
        "\"ravendb\"",
        "Qdrant",
        "\"qdrant\"",
        "Weaviate",
        "\"weaviate\"",
        "AddFlagd",
        "Flagd",
        "\"flagd\"",
        "OpenFeature"
    ];

    public static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ViajantesTurismo.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate repository root.");
    }

    public static string[] FindCandidatePlatformPackageReferenceViolations(string repositoryRoot)
    {
        var appHostProjectPath = Path.Combine(
            repositoryRoot,
            "src",
            "ViajantesTurismo.AppHost",
            "ViajantesTurismo.AppHost.csproj");

        var packageReferences = XDocument.Load(appHostProjectPath)
            .Descendants("PackageReference")
            .Select(element => element.Attribute("Include")?.Value)
            .OfType<string>()
            .Order(StringComparer.Ordinal)
            .ToArray();

        return packageReferences
            .SelectMany(FindCandidatePlatformPackageFragments)
            .ToArray();
    }

    private static string[] FindCandidatePlatformPackageFragments(string packageReference)
    {
        return CandidatePlatformPackageFragments
            .Where(fragment => packageReference.Contains(fragment, StringComparison.OrdinalIgnoreCase))
            .Select(fragment => $"AppHost package reference {packageReference} contains candidate fragment {fragment}")
            .ToArray();
    }

    public static string[] FindCandidatePlatformResourceFragments(string repositoryRoot)
    {
        var appHostDirectory = Path.Combine(repositoryRoot, "src", "ViajantesTurismo.AppHost");

        return Directory.GetFiles(appHostDirectory, "*.cs", SearchOption.TopDirectoryOnly)
            .Order(StringComparer.Ordinal)
            .SelectMany(path => FindCandidatePlatformResourceFragments(path, File.ReadAllText(path)))
            .ToArray();
    }

    private static string[] FindCandidatePlatformResourceFragments(string path, string text)
    {
        return CandidatePlatformResourceFragments
            .Where(fragment => text.Contains(fragment, StringComparison.Ordinal))
            .Select(fragment => $"{Path.GetFileName(path)} contains {fragment}")
            .ToArray();
    }

    [GeneratedRegex(@"public\s+static\s+IResourceBuilder<ProjectResource>\s+AddCatalogApi[\s\S]+?;\s*\n\s*}", RegexOptions.CultureInvariant)]
    public static partial Regex CatalogApiResourceRegex();
}
