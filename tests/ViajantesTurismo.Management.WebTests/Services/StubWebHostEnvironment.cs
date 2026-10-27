using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace ViajantesTurismo.Management.WebTests.Services;

/// <summary>Minimal <see cref="IWebHostEnvironment"/> stub with null file providers and the given web root.</summary>
/// <param name="webRootPath">Initial value of <see cref="WebRootPath"/>.</param>
internal sealed class StubWebHostEnvironment(string webRootPath) : IWebHostEnvironment
{
    /// <summary>Web root directory path.</summary>
    public string WebRootPath { get; set; } = webRootPath;

    /// <summary>File provider for the web root; defaults to <see cref="NullFileProvider"/>.</summary>
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

    /// <summary>Application name; defaults to <c>Test</c>.</summary>
    public string ApplicationName { get; set; } = "Test";

    /// <summary>File provider for the content root; defaults to <see cref="NullFileProvider"/>.</summary>
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();

    /// <summary>Content root directory path; empty by default.</summary>
    public string ContentRootPath { get; set; } = string.Empty;

    /// <summary>Environment name; defaults to <c>Test</c>.</summary>
    public string EnvironmentName { get; set; } = "Test";
}
