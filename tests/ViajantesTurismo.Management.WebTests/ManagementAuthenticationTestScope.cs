using Duende.AccessTokenManagement.OpenIdConnect;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests;

internal sealed class ManagementAuthenticationTestScope : IDisposable
{
    private readonly IServiceScope _scope;

    public ManagementAuthenticationTestScope(IServiceScopeFactory scopeFactory)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        _scope = scopeFactory.CreateScope();
    }

    public IUserTokenStore UserTokenStore => _scope.ServiceProvider.GetRequiredService<IUserTokenStore>();

    public ProtectedDistributedUserTokenStore ProtectedUserTokenStore => _scope.ServiceProvider
        .GetRequiredService<ProtectedDistributedUserTokenStore>();

    public void Dispose()
    {
        _scope.Dispose();
    }
}
