using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace ViajantesTurismo.Management.WebTests.Services;

internal sealed class StubWebHostEnvironment(string webRootPath) : IWebHostEnvironment
{
    public string WebRootPath { get; set; } = webRootPath;

    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

    public string ApplicationName { get; set; } = "Test";

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();

    public string ContentRootPath { get; set; } = string.Empty;

    public string EnvironmentName { get; set; } = "Test";
}
