using System.Security.Cryptography;
using System.Text.Json.Nodes;

namespace SharedKernel.RepoConfig.Tests;

internal sealed class CandidateCompatibilityTestScope : IDisposable
{
    private readonly TemporaryRepoConfigWorkspace workspace = new();

    public CandidateCompatibilityTestScope()
    {
        workspace.WriteFile("Directory.Packages.props", """<Project><PackageVersion Include="SharedKernel.Results" Version="1.0.0" /></Project>""");
        workspace.WriteFile("ViajantesTurismo.slnx", """<Solution><Project Path="Consumer.csproj" /></Solution>""");
        workspace.WriteFile("Consumer.csproj", "<Project />");
        workspace.WriteFile("src/SharedKernel/SharedKernel.Results/SharedKernel.Results.csproj", "<Project />");
        workspace.WriteFile("packages.lock.json", """{"dependencies":{"net10.0":{"SharedKernel.Results":{"type":"Direct","resolved":"1.0.0"},"External.Package":{"type":"Direct","resolved":"2.0.0"}}}}""");
        workspace.WriteFile("SharedKernel.Results.1.0.0.nupkg", "candidate package bytes");
        using var stream = File.OpenRead(Package);
        Evidence = new JsonObject
        {
            ["packageVersion"] = "1.0.0",
            ["libraryDirty"] = false,
            ["packageSha256"] = new JsonObject { [Path.GetFileName(Package)] = Convert.ToHexStringLower(SHA256.HashData(stream)) }
        };
    }

    public string Root => workspace.RootPath;

    public string Package => Path.Combine(Root, "SharedKernel.Results.1.0.0.nupkg");

    public string Manifest => Path.Combine(Root, "candidate.json");

    public JsonObject Evidence { get; }

    public void WriteFile(string path, string content) => workspace.WriteFile(path, content);

    public Task WriteManifest(CancellationToken ct) => File.WriteAllTextAsync(Manifest, Evidence.ToJsonString(), ct);

    public void Dispose() => workspace.Dispose();
}
