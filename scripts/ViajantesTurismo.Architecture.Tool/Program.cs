using ViajantesTurismo.RepositoryDocumentation.Architecture;

if (args is not ["generate" or "verify", var repositoryRoot])
{
    Console.Error.WriteLine("Usage: ViajantesTurismo.Architecture.Tool <generate|verify> <repository-root>");
    return 2;
}

var checkOnly = args[0] == "verify";
var result = ArchitectureDataDiagram.Run(Path.GetFullPath(repositoryRoot), checkOnly);
foreach (var file in result.ChangedFiles)
{
    Console.WriteLine(file);
}

return checkOnly && result.ChangedFiles.Count > 0 ? 1 : 0;
