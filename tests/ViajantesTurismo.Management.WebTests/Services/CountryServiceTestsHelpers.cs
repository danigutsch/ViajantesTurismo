using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace ViajantesTurismo.Management.WebTests.Services;

internal static class CountryServiceTestsHelpers
{
    public static CountryService CreateService(string webRootPath)
    {
        var env = new StubWebHostEnvironment(webRootPath);
        return new CountryService(env);
    }

    public static void WriteCountriesJson(string webRootPath, string json)
    {
        var dataDir = Path.Combine(webRootPath, "data");
        Directory.CreateDirectory(dataDir);
        File.WriteAllText(Path.Combine(dataDir, "countries.json"), json);
    }
}

internal sealed class StubWebHostEnvironment(string webRootPath) : IWebHostEnvironment
{
    public string WebRootPath { get; set; } = webRootPath;

    public IFileProvider WebRootFileProvider { get; set; } = null!;

    public string ApplicationName { get; set; } = "Test";

    public IFileProvider ContentRootFileProvider { get; set; } = null!;

    public string ContentRootPath { get; set; } = string.Empty;

    public string EnvironmentName { get; set; } = "Test";
}
