using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>Loopback Kestrel server whose <c>/token</c> endpoint answers 307 to <c>/redirect-target</c> and records both request bodies.</summary>
internal sealed class RedirectingTokenExchangeTestServer : IAsyncDisposable
{
    private readonly WebApplication _application;
    private readonly ConcurrentQueue<string> _redirectRequestBodies;
    private readonly ConcurrentQueue<string> _tokenRequestBodies;

    private RedirectingTokenExchangeTestServer(
        WebApplication application,
        Uri tokenEndpoint,
        ConcurrentQueue<string> redirectRequestBodies,
        ConcurrentQueue<string> tokenRequestBodies)
    {
        _application = application;
        TokenEndpoint = tokenEndpoint;
        _redirectRequestBodies = redirectRequestBodies;
        _tokenRequestBodies = tokenRequestBodies;
    }

    /// <summary>Absolute URI of the <c>/token</c> endpoint on the bound loopback port.</summary>
    public Uri TokenEndpoint
    {
        get;
    }

    /// <summary>Bodies received by <c>/redirect-target</c>, in arrival order.</summary>
    public IReadOnlyList<string> RedirectRequestBodies => [.. _redirectRequestBodies];

    /// <summary>Bodies received by <c>/token</c>, in arrival order.</summary>
    public IReadOnlyList<string> TokenRequestBodies => [.. _tokenRequestBodies];

    /// <summary>Starts the server on an ephemeral loopback port and returns it once listening.</summary>
    public static async Task<RedirectingTokenExchangeTestServer> Start(CancellationToken ct)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddTestRequestBindingDefaults();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, port: 0));
        var application = builder.Build();
        var redirectRequestBodies = new ConcurrentQueue<string>();
        var tokenRequestBodies = new ConcurrentQueue<string>();
        application.MapPost("/token", async context =>
        {
            tokenRequestBodies.Enqueue(await ReadRequestBody(context.Request, context.RequestAborted));
            context.Response.StatusCode = StatusCodes.Status307TemporaryRedirect;
            context.Response.Headers.Location = "/redirect-target";
        });
        application.MapPost("/redirect-target", async context =>
        {
            redirectRequestBodies.Enqueue(await ReadRequestBody(context.Request, context.RequestAborted));
            context.Response.StatusCode = StatusCodes.Status200OK;
        });

        await application.StartAsync(ct);
        var server = application.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses;
        var address = addresses?.SingleOrDefault()
            ?? throw new InvalidOperationException("The redirecting token-exchange test server has no address.");
        var tokenEndpoint = new Uri(new Uri(address), "token");
        return new RedirectingTokenExchangeTestServer(
            application,
            tokenEndpoint,
            redirectRequestBodies,
            tokenRequestBodies);
    }

    /// <summary>Stops and disposes the underlying web application.</summary>
    public ValueTask DisposeAsync()
    {
        return _application.DisposeAsync();
    }

    private static async Task<string> ReadRequestBody(HttpRequest request, CancellationToken ct)
    {
        using var reader = new StreamReader(request.Body);
        return await reader.ReadToEndAsync(ct);
    }
}
