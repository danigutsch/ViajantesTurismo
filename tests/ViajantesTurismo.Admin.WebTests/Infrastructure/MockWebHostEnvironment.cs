using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace ViajantesTurismo.Admin.WebTests.Infrastructure;

/// <summary>
/// Mock implementation of IWebHostEnvironment for testing purposes.
/// </summary>
public class MockWebHostEnvironment : IWebHostEnvironment
{
    private static readonly string WebProjectPath = GetWebProjectPath();

    public MockWebHostEnvironment()
    {
        WebRootPath = Path.Combine(WebProjectPath, "wwwroot");
        WebRootFileProvider = new PhysicalFileProvider(WebRootPath);
        ContentRootPath = WebProjectPath;
        ContentRootFileProvider = new PhysicalFileProvider(WebProjectPath);
    }

    public string EnvironmentName { get; set; } = "Test";

    public string ApplicationName { get; set; } = "ViajantesTurismo.Admin.Web";

    public string WebRootPath { get; set; }

    public IFileProvider WebRootFileProvider { get; set; }

    public string ContentRootPath { get; set; }

    public IFileProvider ContentRootFileProvider { get; set; }

    private static string GetWebProjectPath()
    {
        // Assembly is at: e:\source\repos\ViajantesTurismo\tests\ViajantesTurismo.Admin.WebTests\bin\Debug\net10.0\
        // We need: e:\source\repos\ViajantesTurismo\src\ViajantesTurismo.Admin.Web\
        var assemblyLocation = typeof(MockWebHostEnvironment).Assembly.Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation) ?? string.Empty;

        // Navigate up from assembly directory to workspace root
        // From: bin Debug net10.0 ViajantesTurismo.Admin.WebTests tests workspace
        var workspacePath = Path.GetFullPath(Path.Combine(assemblyDirectory, "..", "..", "..", "..", ".."));

        // Then navigate to src/ViajantesTurismo.Admin.Web
        return Path.Combine(workspacePath, "src", "ViajantesTurismo.Admin.Web");
    }
}
