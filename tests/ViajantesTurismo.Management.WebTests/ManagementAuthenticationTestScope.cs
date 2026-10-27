using Duende.AccessTokenManagement.OpenIdConnect;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>Disposable service scope exposing the user token stores.</summary>
internal sealed class ManagementAuthenticationTestScope : IDisposable
{
    private readonly IServiceScope _scope;

    /// <summary>Creates a scope from the given factory.</summary>
    public ManagementAuthenticationTestScope(IServiceScopeFactory scopeFactory)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        _scope = scopeFactory.CreateScope();
    }

    /// <summary>User token store resolved from the scope.</summary>
    public IUserTokenStore UserTokenStore => _scope.ServiceProvider.GetRequiredService<IUserTokenStore>();

    /// <summary>Protected distributed user token store resolved from the scope.</summary>
    public ProtectedDistributedUserTokenStore ProtectedUserTokenStore => _scope.ServiceProvider
        .GetRequiredService<ProtectedDistributedUserTokenStore>();

    /// <summary>Disposes the scope.</summary>
    public void Dispose()
    {
        _scope.Dispose();
    }
}
