using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents;

/// <summary>Returns a configurable current source version, matching the draft test data by default.</summary>
internal sealed class FakeDocumentSourceVersionReader(string version = "SOURCE-VERSION") : IDocumentSourceVersionReader
{
    /// <summary>Gets or sets the version returned by subsequent reads.</summary>
    public string Version { get; set; } = version;

    /// <summary>Gets or sets a value indicating whether reads report the booking as missing.</summary>
    public bool SourceMissing
    {
        get; set;
    }

    /// <inheritdoc />
    public Task<Result<string>> Read(Guid bookingId, CancellationToken ct) => Task.FromResult(
        SourceMissing ? BookingErrors.BookingNotFound().ConvertError<string>() : Result.Ok(Version));
}
