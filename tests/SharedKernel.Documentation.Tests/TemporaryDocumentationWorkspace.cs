namespace SharedKernel.Documentation.Tests;

internal sealed class TemporaryDocumentationWorkspace : IDisposable
{
    public TemporaryDocumentationWorkspace()
    {
        RootPath = Path.Combine(Path.GetTempPath(), "sharedkernel-documentation-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(RootPath, "docs", "architecture"));
    }

    public string RootPath { get; }

    public void WriteArchitectureDoc(string fileName, string content) =>
        File.WriteAllText(Path.Combine(RootPath, "docs", "architecture", fileName), content);

    public void WriteConfig(string content) =>
        File.WriteAllText(Path.Combine(RootPath, "docs", "architecture", "generated-diagrams.json"), content);

    public string ReadArchitectureDoc(string fileName) =>
        File.ReadAllText(Path.Combine(RootPath, "docs", "architecture", fileName));

    public void Dispose()
    {
        if (Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, recursive: true);
        }
    }
}
