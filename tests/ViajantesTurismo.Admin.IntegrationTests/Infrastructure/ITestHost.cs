
namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

public interface ITestHost : Xunit.IAsyncLifetime, System.IDisposable
{
    System.Net.Http.HttpClient Client { get; }
    System.Uri BaseUri { get; }
    System.Threading.Tasks.Task Seed();
    System.Threading.Tasks.Task Reset();
}


