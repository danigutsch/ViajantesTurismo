namespace ViajantesTurismo.ArchitectureDocumentation;

/// <summary>An application in the architecture view.</summary>
/// <param name="Id">The stable node identifier.</param>
/// <param name="Label">The application name.</param>
/// <param name="Boundary">The trust boundary of the application.</param>
/// <param name="Audiences">The audiences the application serves.</param>
/// <param name="State">The lifecycle of the application.</param>
internal sealed record ArchitectureService(string Id, string Label, string Boundary, string[] Audiences, string State);
