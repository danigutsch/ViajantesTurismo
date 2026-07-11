using System.Buffers.Binary;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Options;
using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.Catalog.Infrastructure;

/// <summary>
/// Streams uploaded media to a private ClamAV daemon using the INSTREAM protocol.
/// </summary>
internal sealed class ClamAvMediaUploadScanner(IOptions<ClamAvMediaUploadScannerOptions> options) : IMediaUploadScanner
{
    private const string ResponsePrefix = "stream: ";
    private static ReadOnlyMemory<byte> StreamCommand { get; } = "zINSTREAM\0"u8.ToArray();

    private readonly ClamAvMediaUploadScannerOptions _options = options.Value;

    /// <inheritdoc/>
    public async ValueTask<MediaUploadScanResult> Scan(MediaUploadScanRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var started = Stopwatch.GetTimestamp();
        using var activity = ClamAvMediaUploadScannerTelemetry.StartScan();
        using var timeout = new CancellationTokenSource(_options.Timeout);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token);

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_options.Host, _options.Port, cancellation.Token).ConfigureAwait(false);
            using var stream = client.GetStream();
            await stream.WriteAsync(StreamCommand, cancellation.Token).ConfigureAwait(false);
            await WriteContent(stream, request.Content, cancellation.Token).ConfigureAwait(false);

            var response = await ReadResponse(stream, cancellation.Token).ConfigureAwait(false);
            var result = response switch
            {
                var value when value.StartsWith(ResponsePrefix, StringComparison.Ordinal) && value.EndsWith(" OK", StringComparison.Ordinal) => MediaUploadScanResult.Passed,
                var value when value.StartsWith(ResponsePrefix, StringComparison.Ordinal) && value.EndsWith(" FOUND", StringComparison.Ordinal) => new MediaUploadScanResult(MediaUploadScanStatus.Rejected),
                _ => new MediaUploadScanResult(MediaUploadScanStatus.Failed)
            };
            return Complete(activity, result, request.Length, started, result.Status == MediaUploadScanStatus.Failed ? "scanner_protocol_error" : null);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return Complete(activity, new MediaUploadScanResult(MediaUploadScanStatus.Failed), request.Length, started, "timeout");
        }
        catch (SocketException)
        {
            return Complete(activity, new MediaUploadScanResult(MediaUploadScanStatus.Failed), request.Length, started, "scanner_unavailable");
        }
        catch (IOException)
        {
            return Complete(activity, new MediaUploadScanResult(MediaUploadScanStatus.Failed), request.Length, started, "scanner_protocol_error");
        }
    }

    private static MediaUploadScanResult Complete(Activity? activity, MediaUploadScanResult result, long length, long started, string? errorType)
    {
        ClamAvMediaUploadScannerTelemetry.Record(activity, result.Status, length, Stopwatch.GetElapsedTime(started), errorType);
        return result;
    }

    private async Task WriteContent(Stream target, Stream content, CancellationToken ct)
    {
        var buffer = new byte[_options.ChunkSize];
        var length = new byte[sizeof(int)];
        while (true)
        {
            var bytesRead = await content.ReadAsync(buffer, ct).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                break;
            }

            BinaryPrimitives.WriteInt32BigEndian(length, bytesRead);
            await target.WriteAsync(length, ct).ConfigureAwait(false);
            await target.WriteAsync(buffer.AsMemory(0, bytesRead), ct).ConfigureAwait(false);
        }

        await target.WriteAsync(new byte[sizeof(int)], ct).ConfigureAwait(false);
        await target.FlushAsync(ct).ConfigureAwait(false);
    }

    private static async Task<string> ReadResponse(Stream stream, CancellationToken ct)
    {
        using var response = new MemoryStream();
        var buffer = new byte[256];
        while (true)
        {
            var bytesRead = await stream.ReadAsync(buffer, ct).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                throw new IOException("ClamAV closed the connection without a scan response.");
            }

            var terminator = Array.IndexOf(buffer, (byte)0, 0, bytesRead);
            if (terminator >= 0)
            {
                await response.WriteAsync(buffer.AsMemory(0, terminator), ct).ConfigureAwait(false);
                if (response.Length > 4 * 1024)
                {
                    throw new IOException("ClamAV returned an oversized scan response.");
                }

                return Encoding.ASCII.GetString(response.GetBuffer(), 0, checked((int)response.Length));
            }

            await response.WriteAsync(buffer.AsMemory(0, bytesRead), ct).ConfigureAwait(false);
            if (response.Length > 4 * 1024)
            {
                throw new IOException("ClamAV returned an oversized scan response.");
            }
        }
    }
}
