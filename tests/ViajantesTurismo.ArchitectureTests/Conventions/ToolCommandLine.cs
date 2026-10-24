namespace ViajantesTurismo.ArchitectureTests.Conventions;

internal sealed record ToolCommandLine(string Source, string Command, IReadOnlyList<string> Arguments);
