using System.Buffers;
using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using PandaBear.IO;

const byte TargetByte = 0x5A;
const int BufferLength = 64 * 1024;
const int CopyBufferLength = 80 * 1024;
const int SpoolMemoryThresholdBytes = 1024 * 1024;
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
app.MapPost("/upload/scan-buffered", ScanBufferedUpload).DisableAntiforgery();
app.MapPost("/upload/scan-seekable", ScanSeekableUpload).DisableAntiforgery();
app.MapGet("/diagnostics", static () => Results.Text(
    string.Create(
        CultureInfo.InvariantCulture,
        $"allocated_bytes={GC.GetTotalAllocatedBytes()};gen0={GC.CollectionCount(0)};gen1={GC.CollectionCount(1)};gen2={GC.CollectionCount(2)};pause_ms={GC.GetTotalPauseDuration().TotalMilliseconds:F1}"),
    "text/plain"));

app.Lifetime.ApplicationStarted.Register(() => WriteReadyFile(app, Environment.GetEnvironmentVariable(ReadyFileVariable)));

await app.RunAsync().ConfigureAwait(false);

static async Task<IResult> ScanUpload(HttpRequest request, CancellationToken ct)
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
    while ((section = await reader.ReadNextSectionAsync(ct).ConfigureAwait(false)) is not null)
    {
        if (!IsFileSection(section))
        {
            continue;
        }

        var (BytesScanned, Matches) = await ScanStream(section.Body, ct).ConfigureAwait(false);
        totalBytesScanned += BytesScanned;
        totalMatches += Matches;
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

// Mirrors the production intake shape: IFormFile binding, one presized MemoryStream filled by a bounded copy loop,
// one scan pass and one SHA-256 pass. It has no decode, object store, scanner, rate limiter, or authentication.
static async Task<IResult> ScanBufferedUpload(IFormFile? file, CancellationToken ct)
{
    if (file is null || file.Length is <= 0 or > MaxPayloadBytes)
    {
        return Results.BadRequest("A multipart file section named file within the size limit is required.");
    }

    var buffer = new MemoryStream(checked((int)file.Length));
    await using var bufferScope = buffer.ConfigureAwait(false);
    var rented = ArrayPool<byte>.Shared.Rent(CopyBufferLength);

    try
    {
        var source = file.OpenReadStream();
        await using var sourceScope = source.ConfigureAwait(false);
        long received = 0;
        int bytesRead;

        while ((bytesRead = await source.ReadAsync(rented.AsMemory(0, CopyBufferLength), ct).ConfigureAwait(false)) > 0)
        {
            received += bytesRead;

            if (received > MaxPayloadBytes)
            {
                return Results.BadRequest("The file exceeds the size limit.");
            }

            await buffer.WriteAsync(rented.AsMemory(0, bytesRead), ct).ConfigureAwait(false);
        }

        if (received != file.Length)
        {
            return Results.BadRequest("The received length does not match the declared length.");
        }
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(rented);
    }

    buffer.Position = 0;
    var (bytesScanned, matches) = await ScanStream(buffer, ct).ConfigureAwait(false);

    buffer.Position = 0;
    var hash = Convert.ToHexString(await SHA256.HashDataAsync(buffer, ct).ConfigureAwait(false));

    return Results.Text(
        string.Create(CultureInfo.InvariantCulture, $"files=1;bytes={bytesScanned};matches={matches};sha256={hash}"),
        "text/plain");
}

// Mirrors the adopted production intake shape: IFormFile binding, SeekableContent counting pass (the buffered IFormFile stream is
// passed through uncopied), one scan pass and one SHA-256 pass. It has no decode, object store, real scanner, rate limiter, or authentication.
static async Task<IResult> ScanSeekableUpload(IFormFile? file, CancellationToken ct)
{
    if (file is null || file.Length is <= 0 or > MaxPayloadBytes)
    {
        return Results.BadRequest("A multipart file section named file within the size limit is required.");
    }

    var source = file.OpenReadStream();
    await using var sourceScope = source.ConfigureAwait(false);
    SeekableContent lease;

    try
    {
        lease = await SeekableContent.Create(source, MaxPayloadBytes, SpoolMemoryThresholdBytes, ct).ConfigureAwait(false);
    }
    catch (InvalidDataException)
    {
        return Results.BadRequest("The file exceeds the size limit or its received length does not match.");
    }

    await using var leaseScope = lease.ConfigureAwait(false);
    var content = lease.Content;

    if (content.Length != file.Length)
    {
        return Results.BadRequest("The received length does not match the declared length.");
    }

    var (bytesScanned, matches) = await ScanStream(content, ct).ConfigureAwait(false);

    content.Position = 0;
    var hash = Convert.ToHexString(await SHA256.HashDataAsync(content, ct).ConfigureAwait(false));

    return Results.Text(
        string.Create(CultureInfo.InvariantCulture, $"files=1;bytes={bytesScanned};matches={matches};sha256={hash}"),
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

static async Task<(long BytesScanned, long Matches)> ScanStream(Stream stream, CancellationToken ct)
{
    var buffer = GC.AllocateUninitializedArray<byte>(BufferLength);
    long bytesScanned = 0;
    long matches = 0;

    while (true)
    {
        var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct).ConfigureAwait(false);

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
