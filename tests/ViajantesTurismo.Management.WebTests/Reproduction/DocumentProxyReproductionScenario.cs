using SharedKernel.Testing.Snapshots;
using ViajantesTurismo.Testing.Reproduction;

namespace ViajantesTurismo.Management.WebTests.Reproduction;

/// <summary>Registers the approved document proxy outcomes at the compiled fixture revision.</summary>
internal static class DocumentProxyReproductionScenario
{
    /// <summary>The outcome for an authorized private HTML attachment.</summary>
    internal const string AttachmentDelivered = "attachment-delivered";

    /// <summary>The outcome for a signed-in operator whose role cannot download documents.</summary>
    internal const string Forbidden = "forbidden";

    /// <summary>The approved outcomes and resource states for the named fixture.</summary>
    internal static ReproductionSpecification Specification
    {
        get;
    } = new(
        "management.document-download",
        ReproductionFixtureEnvironment.GetSourceRevision(typeof(DocumentProxyReproductionScenario).Assembly),
        [
            new("reproduced", AttachmentDelivered, new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["management-web"] = "attachment-delivered"
            }),
            new("reproduced", Forbidden, new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["management-web"] = "access-denied"
            })
        ]);
}
