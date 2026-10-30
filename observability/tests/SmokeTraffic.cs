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
    ["catalog-api"] = ["/health"],
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

    for (var round = 0; round < 6; round++)
    {
        foreach (var path in match.Value)
        {
            try
            {
                using var response = await http.GetAsync(baseUrl + path);
            }
            catch (HttpRequestException)
            {
                // Only reachability matters; a refused connection simply yields no traffic.
            }
        }
    }

    Console.WriteLine($"sent {name} {string.Join(' ', match.Value)}");
}

return 0;

static bool AcceptLocalDevelopmentCertificate(HttpRequestMessage request, System.Security.Cryptography.X509Certificates.X509Certificate2? certificate, System.Security.Cryptography.X509Certificates.X509Chain? chain, System.Net.Security.SslPolicyErrors errors) =>
    request.RequestUri is { IsLoopback: true };
