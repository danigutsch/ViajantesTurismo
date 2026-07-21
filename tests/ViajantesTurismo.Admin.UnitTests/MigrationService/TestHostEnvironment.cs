using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace ViajantesTurismo.Admin.UnitTests.MigrationService;

internal sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = environmentName;

    public string ApplicationName { get; set; } = "ViajantesTurismo.MigrationService.Tests";

    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
