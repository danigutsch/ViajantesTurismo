using ViajantesTurismo.Admin.ApiService;

[assembly: AssemblyFixture(typeof(ApiFixture))]

namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

public abstract class AdminApiIntegrationTestBase : IClassFixture<IAdminTestHost>, IDisposable
{
    protected readonly IAdminTestHost Host;
    public AdminApiIntegrationTestBase(IAdminTestHost host)
        : base(host) { Host = host; }
    public void Dispose() { }
}
