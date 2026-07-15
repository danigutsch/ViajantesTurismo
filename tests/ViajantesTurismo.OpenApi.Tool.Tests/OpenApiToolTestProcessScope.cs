using System.Diagnostics;
using System.ComponentModel;

namespace ViajantesTurismo.OpenApi.Tool.Tests;

internal sealed class OpenApiToolTestProcessScope : IDisposable
{
    private CancellationTokenSource? cancellationOnStart;

    public OpenApiToolTestProcessScope(bool forceDocumentGeneration = false)
    {
        GeneratedDirectory = Path.Combine(Path.GetTempPath(), $"viajantes-openapi-{Guid.CreateVersion7():N}");
        Directory.CreateDirectory(GeneratedDirectory);
        this.forceDocumentGeneration = forceDocumentGeneration;
    }

    private readonly bool forceDocumentGeneration;

    public string GeneratedDirectory { get; }

    public bool ChildStarted { get; private set; }

    public int ChildProcessId { get; private set; }

    public void CancelAfterChildStart(CancellationTokenSource cancellation)
    {
        ArgumentNullException.ThrowIfNull(cancellation);

        cancellationOnStart = cancellation;
    }

    public Process? Start(ProcessStartInfo startInfo)
    {
        ArgumentNullException.ThrowIfNull(startInfo);

        if (forceDocumentGeneration)
        {
            var projectPath = startInfo.ArgumentList[2];
            startInfo.ArgumentList.Clear();
            startInfo.ArgumentList.Add("msbuild");
            startInfo.ArgumentList.Add(projectPath);
            startInfo.ArgumentList.Add("-t:Build;GenerateOpenApiDocuments");
            startInfo.ArgumentList.Add("-p:OpenApiGenerateDocuments=true");
            startInfo.ArgumentList.Add("-p:OpenApiGenerateDocumentsOnBuild=false");
            startInfo.ArgumentList.Add($"-p:OpenApiDocumentsDirectory={GeneratedDirectory}");
            startInfo.ArgumentList.Add($"-p:_OpenApiDocumentsCache={Path.Combine(GeneratedDirectory, "OpenApiFiles.cache")}");
        }
        else
        {
            startInfo.ArgumentList.Add($"-p:OpenApiDocumentsDirectory={GeneratedDirectory}");
        }

        var child = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start the test child process.");
        ChildStarted = true;
        ChildProcessId = child.Id;
        cancellationOnStart?.CancelAfter(TimeSpan.Zero);
        return child;
    }

    public void Dispose()
    {
        if (Directory.Exists(GeneratedDirectory))
        {
            Directory.Delete(GeneratedDirectory, recursive: true);
        }
    }

    public static Process? ReturnNoProcess(ProcessStartInfo startInfo)
    {
        ArgumentNullException.ThrowIfNull(startInfo);

        return null;
    }

    public static Process? ThrowProcessStartFailure(ProcessStartInfo startInfo)
    {
        ArgumentNullException.ThrowIfNull(startInfo);

        throw new Win32Exception("The test process could not start.");
    }

    public static Process? ReturnUnstartedProcess(ProcessStartInfo startInfo)
    {
        ArgumentNullException.ThrowIfNull(startInfo);

        return new Process();
    }
}
