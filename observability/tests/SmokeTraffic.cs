#:property TreatWarningsAsErrors=false
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA2007;CA2234;CA1849;CA1307

// Sends a little traffic to the running AppHost so dashboard panels have data to verify.
// Needs a running `ASPIRE_ENABLE_OBSERVABILITY_STACK=1 dotnet tool run aspire run` session.
//
// Run: dotnet run observability/tests/SmokeTraffic.cs

using System.Diagnostics;
using System.Text.Json;

var paths = new Dictionary<string, string[]>
{
    ["public-web-app"] = ["/", "/tours"],
    ["web-app"] = ["/"],
    ["catalog-api"] = ["/health", "/api/v1/public/branding"],
    ["api-"] = ["/health"],
};

var describe = Process.Start(new ProcessStartInfo("dotnet", "tool run aspire describe --format json")
{
    RedirectStandardOutput = true,
})!;
var output = await describe.StandardOutput.ReadToEndAsync();
await describe.WaitForExitAsync();
if (describe.ExitCode != 0)
{
    Console.Error.WriteLine("aspire describe failed; is the AppHost running?");
    return 1;
}

using var document = JsonDocument.Parse(output[output.IndexOf('{', StringComparison.Ordinal)..]);

using var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = AcceptLocalDevelopmentCertificate };
using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };

var totalReached = 0;

foreach (var resource in document.RootElement.GetProperty("resources").EnumerateArray())
{
    var name = resource.GetProperty("name").GetString()!;
    var match = paths.FirstOrDefault(pair => name.StartsWith(pair.Key, StringComparison.Ordinal));
    if (match.Key is null || !resource.TryGetProperty("urls", out var urls))
    {
        continue;
    }

    var baseUrl = urls.EnumerateArray()
        .Select(url => url.GetProperty("url").GetString()!)
        .FirstOrDefault(url => url.StartsWith("https://", StringComparison.Ordinal));
    if (baseUrl is null)
    {
        continue;
    }

    var reached = 0;
    for (var round = 0; round < 6; round++)
    {
        foreach (var path in match.Value)
        {
            if (await TryGet(baseUrl + path))
            {
                reached++;
            }
        }
    }

    Console.WriteLine($"{(reached > 0 ? "sent" : "UNREACHABLE")} {name} {string.Join(' ', match.Value)} ({reached} of {6 * match.Value.Length} reachable)");
    totalReached += reached;
}

await SendBookingsCommandTraffic([.. document.RootElement.GetProperty("resources").EnumerateArray()]);

return totalReached > 0 ? 0 : 1;

// Drives Bookings commands through SharedKernel.Mediator with an authenticated Keycloak conformance token.
// The empty id hits BookingIdGuardBehavior and the random id reaches the handler; both end as 404 with mediator.outcome=success.
async Task SendBookingsCommandTraffic(JsonElement[] resources)
{
    var api = resources.FirstOrDefault(resource => resource.GetProperty("name").GetString()!.StartsWith("api-", StringComparison.Ordinal));
    var identityProvider = resources.FirstOrDefault(resource =>
        resource.GetProperty("name").GetString()!.StartsWith("identity-provider-", StringComparison.Ordinal)
        && resource.TryGetProperty("urls", out var providerUrls)
        && providerUrls.EnumerateArray().Any(url => url.GetProperty("url").GetString()!.StartsWith("http://", StringComparison.Ordinal)));
    if (api.ValueKind == JsonValueKind.Undefined || identityProvider.ValueKind == JsonValueKind.Undefined)
    {
        Console.WriteLine("skipped bookings command traffic: api or identity provider missing");
        return;
    }

    var password = await ReadConformancePassword(identityProvider.GetProperty("name").GetString()!);
    if (password is null)
    {
        Console.WriteLine("skipped bookings command traffic: container runtime or conformance password unavailable");
        return;
    }

    var authority = identityProvider.GetProperty("urls").EnumerateArray()
        .Select(url => url.GetProperty("url").GetString()!)
        .First(url => url.StartsWith("http://", StringComparison.Ordinal));
    using var form = new FormUrlEncodedContent(new Dictionary<string, string>
    {
        ["grant_type"] = "password",
        ["client_id"] = "conformance-test-client",
        ["username"] = "conformance",
        ["password"] = password,
        ["scope"] = "admin-api",
    });
    using var tokenResponse = await http.PostAsync(authority + "/realms/viajantes/protocol/openid-connect/token", form);
    tokenResponse.EnsureSuccessStatusCode();
    using var tokenDocument = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
    var token = tokenDocument.RootElement.GetProperty("access_token").GetString()!;

    var baseUrl = api.GetProperty("urls").EnumerateArray()
        .Select(url => url.GetProperty("url").GetString()!)
        .First(url => url.StartsWith("https://", StringComparison.Ordinal));
    foreach (var _ in Enumerable.Range(0, 3))
    {
        foreach (var bookingId in new[] { Guid.Empty, Guid.NewGuid() })
        {
            foreach (var action in new[] { "complete", "cancel" })
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/v1/bookings/{bookingId}/{action}");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                using var response = await http.SendAsync(request);
            }
        }
    }

    Console.WriteLine($"sent {api.GetProperty("name").GetString()} bookings complete/cancel commands");
}

static async Task<string?> ReadConformancePassword(string containerName)
{
    foreach (var runtime in new[] { "podman", "docker" })
    {
        try
        {
            using var inspect = Process.Start(new ProcessStartInfo(runtime, ["inspect", containerName, "--format", "{{json .Config.Env}}"])
            {
                RedirectStandardOutput = true,
            })!;
            var json = await inspect.StandardOutput.ReadToEndAsync();
            await inspect.WaitForExitAsync();
            if (inspect.ExitCode != 0)
            {
                continue;
            }

            var entry = JsonSerializer.Deserialize<string[]>(json)?.FirstOrDefault(value => value.StartsWith("LOCAL_CONFORMANCE_PASSWORD=", StringComparison.Ordinal));
            if (entry is not null)
            {
                return entry["LOCAL_CONFORMANCE_PASSWORD=".Length..];
            }
        }
        catch (System.ComponentModel.Win32Exception)
        {
        }
    }

    return null;
}

async Task<bool> TryGet(string url)
{
    try
    {
        using var response = await http.GetAsync(url);
        return true;
    }
    catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
    {
        return false;
    }
}

static bool AcceptLocalDevelopmentCertificate(HttpRequestMessage request, System.Security.Cryptography.X509Certificates.X509Certificate2? certificate, System.Security.Cryptography.X509Certificates.X509Chain? chain, System.Net.Security.SslPolicyErrors errors) =>
    request.RequestUri is { IsLoopback: true };
