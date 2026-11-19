using ViajantesTurismo.AsyncApi.Tool;

if (args is not ["generate" or "verify", var artifactPath])
{
    Console.Error.WriteLine("Usage: ViajantesTurismo.AsyncApi.Tool <generate|verify> <artifact-path>");
    return 2;
}

var generated = AsyncApiDocument.Render();
if (args[0] == "generate")
{
    File.WriteAllText(artifactPath, generated);
    return 0;
}

if (File.Exists(artifactPath) && string.Equals(File.ReadAllText(artifactPath), generated, StringComparison.Ordinal))
{
    return 0;
}

Console.Error.WriteLine($"AsyncAPI contract drift in '{artifactPath}'. Run the generate command to refresh the artifact.");
return 1;
