using System.Xml.Linq;
using System.Diagnostics;
using System.IO.Pipes;

namespace ViajantesTurismo.RepoConfig.Tests;

internal sealed class CandidateScratchTestScope : IDisposable, IAsyncDisposable
{
    private readonly StringWriter output = new();
    private readonly StringWriter error = new();
    private CancellationTokenSource? cancellation;
    private NamedPipeServerStream? pipe;
    public Task<int> Completion { get; private set; } = Task.FromResult(0);

    public async Task<Process> StartBlockedRestore(string destination, CancellationToken ct)
    {
        var pipeName = Guid.NewGuid().ToString("N");
        // Absolute Unix paths avoid the runtime's extra CoreFxPipe_ prefix under long temporary roots.
        if (!OperatingSystem.IsWindows()) { pipeName = Path.Combine(Path.GetTempPath(), pipeName); }
        pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        BlockRestore(pipeName);
        await Prepare(ct);
        cancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        Completion = CandidateCompatibilityCommand.Run(
            ["--candidate", Candidate.Manifest, "--output", destination], output, error, Candidate.Root, cancellation.Token);
        var connection = pipe.WaitForConnectionAsync(ct);
        var ready = await Task.WhenAny(connection, Completion);
        if (ready != connection) { throw new InvalidOperationException(error.ToString()); }
        await connection;
        using var reader = new StreamReader(pipe, leaveOpen: true);
        var processId = await reader.ReadLineAsync(ct);
        processId.ShouldNotBeNull();
        return Process.GetProcessById(int.Parse(processId, System.Globalization.CultureInfo.InvariantCulture));
    }

    public Task Cancel() => cancellation.ShouldNotBeNull().CancelAsync();

    public async ValueTask DisposeAsync()
    {
        if (cancellation is not null)
        {
            await cancellation.CancelAsync();
            try { await Completion; }
            catch (OperationCanceledException) { /* Expected termination of the blocked restore. */ }
            cancellation.Dispose();
        }

        Dispose();
    }

    public CandidateCompatibilityTestScope Candidate { get; } = new();

    public void BlockRestore(string pipeName)
    {
        Candidate.WriteFile("Consumer.csproj", $$"""
            <Project>
              <UsingTask TaskName="HoldCache" TaskFactory="RoslynCodeTaskFactory"
                         AssemblyFile="$(MSBuildToolsPath)/Microsoft.Build.Tasks.Core.dll">
                <ParameterGroup><Cache ParameterType="System.String" Required="true" /></ParameterGroup>
                <Task>
                  <Code Type="Fragment" Language="cs"><![CDATA[
                    var found = false;
                    foreach (var directory in System.IO.Directory.GetDirectories(System.IO.Path.GetTempPath(), "sharedkernel-consumer-*"))
                    {
                        var configuration = System.IO.Path.Combine(directory, "NuGet.config");
                        if (!System.IO.File.Exists(configuration)) { continue; }
                        var document = new System.Xml.XmlDocument();
                        document.Load(configuration);
                        if (document.SelectSingleNode("/configuration/packageSources/add[@key='local']/@value")?.Value == Cache)
                        {
                            Cache = System.IO.Path.Combine(directory, "packages");
                            found = true;
                            break;
                        }
                    }
                    if (!found) { throw new System.InvalidOperationException("Isolated cache configuration was not found."); }
                    System.IO.Directory.CreateDirectory(Cache);
                    using (var cacheFile = System.IO.File.Open(System.IO.Path.Combine(Cache, "held.cache"), System.IO.FileMode.Create, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None))
                    using (var pipe = new System.IO.Pipes.NamedPipeClientStream(".", "{{pipeName}}", System.IO.Pipes.PipeDirection.InOut))
                    {
                        pipe.Connect();
                        using (var writer = new System.IO.StreamWriter(pipe, System.Text.Encoding.UTF8, 1024, true))
                        {
                            writer.AutoFlush = true;
                            writer.WriteLine(System.Diagnostics.Process.GetCurrentProcess().Id);
                            using (var reader = new System.IO.StreamReader(pipe)) { reader.ReadLine(); }
                        }
                    }
                  ]]></Code>
                </Task>
              </UsingTask>
              <Target Name="Restore"><HoldCache Cache="$(MSBuildProjectDirectory)" /></Target>
            </Project>
            """);
    }

    public async Task Prepare(CancellationToken ct)
    {
        Candidate.WriteFile(".gitignore", "evidence/\n");
        await Candidate.WriteManifest(ct);
        string[][] commands = [
            ["init"], ["add", "."],
            ["-c", "user.name=Compatibility Test", "-c", "user.email=compatibility@example.test", "-c", "commit.gpgsign=false", "commit", "-m", "test fixture"]
        ];
        foreach (var command in commands)
        {
            var result = await CiTestSelectionTestProcess.Run("git", Candidate.Root, command, ct);
            result.ExitCode.ShouldBe(0);
        }
    }

    public string[] FindScratchDirectories() => Directory.GetDirectories(Path.GetTempPath(), "sharedkernel-consumer-*")
        .Where(directory =>
        {
            var configuration = Path.Combine(directory, "NuGet.config");
            return File.Exists(configuration) && XDocument.Load(configuration).Descendants("add")
                .Any(source => (string?)source.Attribute("value") == Candidate.Root);
        }).ToArray();

    public void Dispose()
    {
        foreach (var directory in FindScratchDirectories())
        {
            Directory.Delete(directory, recursive: true);
        }

        Candidate.Dispose();
        pipe?.Dispose();
        output.Dispose();
        error.Dispose();
    }
}
