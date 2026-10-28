using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ViajantesTurismo.UploadWire.Testing;

/// <summary>
/// Raw HTTP/1.1 client that declares the full body length, sends only a prefix of it, and then aborts the TCP
/// connection, so the server sees a client that vanished mid-upload.
/// </summary>
public sealed class RawHttpUpload : IDisposable
{
    private readonly TcpClient connection = new();

    /// <summary>Connects, sends the headers and the first <paramref name="bytesToSend"/> body bytes.</summary>
    public async Task Send(Uri baseUri, string path, string authorization, MultipartPayload payload, int bytesToSend, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(baseUri);
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(bytesToSend, payload.Body.Length);

        await connection.ConnectAsync(IPAddress.Loopback, baseUri.Port, ct).ConfigureAwait(false);
        var head = $"POST {path} HTTP/1.1\r\nHost: {baseUri.Authority}\r\nAuthorization: {authorization}\r\nContent-Type: {payload.ContentType}\r\nContent-Length: {payload.Body.Length}\r\n\r\n";
        var stream = connection.GetStream();
        await stream.WriteAsync(Encoding.ASCII.GetBytes(head), ct).ConfigureAwait(false);
        await stream.WriteAsync(payload.Body[..bytesToSend], ct).ConfigureAwait(false);
        await stream.FlushAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Reads the response status line the server sent on this connection.</summary>
    public async Task<string> ReadStatusLine(CancellationToken ct)
    {
        using var reader = new StreamReader(connection.GetStream(), Encoding.ASCII, leaveOpen: true);
        return await reader.ReadLineAsync(ct).ConfigureAwait(false) ?? string.Empty;
    }

    /// <summary>Aborts the connection with a reset instead of a graceful close.</summary>
    public void Abort()
    {
        connection.LingerState = new LingerOption(true, 0);
        connection.Close();
    }

    /// <inheritdoc />
    public void Dispose() => connection.Dispose();
}
