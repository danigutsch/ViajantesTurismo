using System.Globalization;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

const byte TargetByte = 0x5A;
const int BufferLength = 64 * 1024;
const long MaxPayloadBytes = 16_777_216;
const long MaxMultipartOverheadBytes = 1_048_576;
const long MaxRequestBodyBytes = MaxPayloadBytes + MaxMultipartOverheadBytes;
const int MaxBoundaryLength = 128;
const string ReadyFileVariable = "VT_UPLOAD_BENCHMARK_READY_FILE";
const string MultipartContentType = "multipart/form-data";

var builder = WebApplication.CreateSlimBuilder(args);
builder.WebHost.UseKestrel(static options =>
{
    options.Limits.MaxRequestBodySize = MaxRequestBodyBytes;
});

var app = builder.Build();

app.MapGet("/health", static () => Results.Text("ok", "text/plain"));
app.MapPost("/upload/scan", ScanUpload);

app.Lifetime.ApplicationStarted.Register(() => WriteReadyFile(app, Environment.GetEnvironmentVariable(ReadyFileVariable)));

await app.RunAsync().ConfigureAwait(false);

static async Task<IResult> ScanUpload(HttpRequest request, CancellationToken cancellationToken)
{
    if (!TryGetBoundary(request, out var boundary))
    {
        return Results.BadRequest("multipart/form-data content with a valid boundary is required.");
    }

    var reader = new MultipartReader(boundary, request.Body);
    long totalBytesScanned = 0;
    long totalMatches = 0;
    var fileCount = 0;

    MultipartSection? section;
    while ((section = await reader.ReadNextSectionAsync(cancellationToken).ConfigureAwait(false)) is not null)
    {
        if (!IsFileSection(section))
        {
            continue;
        }

        var scanResult = await ScanStream(section.Body, cancellationToken).ConfigureAwait(false);
        totalBytesScanned += scanResult.BytesScanned;
        totalMatches += scanResult.Matches;
        fileCount++;
    }

    if (fileCount == 0)
    {
        return Results.BadRequest("A multipart file section named file is required.");
    }

    return Results.Text(
        string.Create(CultureInfo.InvariantCulture, $"files={fileCount};bytes={totalBytesScanned};matches={totalMatches}"),
        "text/plain");
}

static bool TryGetBoundary(HttpRequest request, out string boundary)
{
    boundary = string.Empty;

    if (request.ContentType is null || !MediaTypeHeaderValue.TryParse(request.ContentType, out var contentType))
    {
        return false;
    }

    if (!string.Equals(contentType.MediaType.Value, MultipartContentType, StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value ?? string.Empty;

    return !string.IsNullOrWhiteSpace(boundary) && boundary.Length <= MaxBoundaryLength;
}

static bool IsFileSection(MultipartSection section)
{
    if (section.ContentDisposition is null || !ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition))
    {
        return false;
    }

    if (!string.Equals(contentDisposition.DispositionType.Value, "form-data", StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    if (!string.Equals(contentDisposition.Name.Value?.Trim('"'), "file", StringComparison.Ordinal))
    {
        return false;
    }

    return contentDisposition.FileName.HasValue || contentDisposition.FileNameStar.HasValue;
}

static async Task<(long BytesScanned, long Matches)> ScanStream(Stream stream, CancellationToken cancellationToken)
{
    var buffer = GC.AllocateUninitializedArray<byte>(BufferLength);
    long bytesScanned = 0;
    long matches = 0;

    while (true)
    {
        var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);

        if (bytesRead == 0)
        {
            break;
        }

        bytesScanned += bytesRead;
        matches += CountMatches(buffer.AsSpan(0, bytesRead));
    }

    return (bytesScanned, matches);
}

static long CountMatches(ReadOnlySpan<byte> source)
{
    long matches = 0;

    foreach (var value in source)
    {
        if (value == TargetByte)
        {
            matches++;
        }
    }

    return matches;
}

static void WriteReadyFile(WebApplication app, string? readyFilePath)
{
    if (string.IsNullOrWhiteSpace(readyFilePath))
    {
        return;
    }

    var server = app.Services.GetRequiredService<IServer>();
    var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses ?? app.Urls;
    var address = addresses.FirstOrDefault(static value => value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) ?? addresses.FirstOrDefault();

    if (string.IsNullOrWhiteSpace(address))
    {
        return;
    }

    try
    {
        var directory = Path.GetDirectoryName(readyFilePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(readyFilePath, address);
    }
    catch (IOException exception)
    {
        ReportReadyFileWriteFailure(readyFilePath, exception);
    }
    catch (UnauthorizedAccessException exception)
    {
        ReportReadyFileWriteFailure(readyFilePath, exception);
    }
    catch (ArgumentException exception)
    {
        ReportReadyFileWriteFailure(readyFilePath, exception);
    }
    catch (NotSupportedException exception)
    {
        ReportReadyFileWriteFailure(readyFilePath, exception);
    }
}

static void ReportReadyFileWriteFailure(string readyFilePath, Exception exception)
{
    Console.Error.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Could not write benchmark ready file '{readyFilePath}': {exception.Message}"));
}
