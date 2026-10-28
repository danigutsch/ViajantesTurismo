using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace ViajantesTurismo.UploadWire.Testing;

/// <summary>Runs a test factory on a real Kestrel socket bound to the loopback interface.</summary>
public static class LoopbackKestrel
{
    /// <summary>Binds the host to a random free loopback port; call it on the builder of the factory that is started.</summary>
    public static void UseRandomLoopbackPort(this IWebHostBuilder host)
    {
        ArgumentNullException.ThrowIfNull(host);

        _ = host.UseUrls("http://127.0.0.1:0");
    }

    /// <summary>Starts the factory's host on Kestrel (random port) and returns its base address.</summary>
    public static Uri Start<T>(this WebApplicationFactory<T> factory)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(factory);

        factory.UseKestrel(0);
        factory.StartServer();
        var addresses = factory.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()
            ?? throw new InvalidOperationException("The server publishes no addresses.");
        return new Uri(addresses.Addresses.First().Replace("[::]", "127.0.0.1", StringComparison.Ordinal).Replace("0.0.0.0", "127.0.0.1", StringComparison.Ordinal));
    }
}
