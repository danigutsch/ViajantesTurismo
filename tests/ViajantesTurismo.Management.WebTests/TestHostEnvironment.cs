using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace ViajantesTurismo.Management.WebTests;

internal sealed class TestHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Production;

    public string ApplicationName { get; set; } = "ViajantesTurismo.Management.WebTests";

    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
