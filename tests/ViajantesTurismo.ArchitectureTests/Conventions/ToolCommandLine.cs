namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>A SharedKernel tool command line found in a repository file.</summary>
/// <param name="Source">The repository-relative path of the file that contains the command line.</param>
/// <param name="Command">The name of the tool command.</param>
/// <param name="Arguments">The arguments passed to the tool command.</param>
internal sealed record ToolCommandLine(string Source, string Command, IReadOnlyList<string> Arguments);
