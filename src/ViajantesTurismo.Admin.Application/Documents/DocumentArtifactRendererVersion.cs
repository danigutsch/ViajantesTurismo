using System.Reflection;
using PandaBear.DocumentRendering;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Captures the build identities of the Admin render adapter and its HTML renderer.</summary>
internal static class DocumentArtifactRendererVersion
{
    /// <summary>Gets the version information recorded on a newly rendered artifact.</summary>
    internal static string Current
    {
        get;
    } = $"Admin {ReadVersion(typeof(DocumentArtifactRendererVersion).Assembly)}, HTML {ReadVersion(typeof(HtmlDocumentRenderer).Assembly)}";

    private static string ReadVersion(Assembly assembly) =>
        assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? assembly.GetName().Version?.ToString()
        ?? "unreported";
}
