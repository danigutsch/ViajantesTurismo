using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using PandaBear.BuildingBlocks;
using PandaBear.ImageProcessing;
using PandaBear.IO;
using PandaBear.MalwareScanning;
using PandaBear.Results;

namespace ViajantesTurismo.Branding.Infrastructure;

/// <summary>
/// Turns an uploaded image into a Branding logo: validates the declared type and signature, bounds the size, scans
/// for malware, decodes, and re-encodes it so metadata is stripped and dimensions and bytes are bounded.
/// </summary>
/// <param name="scanner">The malware scanner.</param>
/// <param name="logger">The logger.</param>
public sealed partial class BrandingLogoIntake(IMalwareScanner scanner, ILogger<BrandingLogoIntake> logger)
{
    private const string InvalidUploadDetail = "Logo upload is invalid.";
    private const string ScannerUnavailableDetail = "Logo upload scanner is unavailable.";
    private const string ContentField = "Content";
    private const int SignatureLength = 12;

    private static readonly ImageProcessingLimits UploadLimits = new(
        BrandingLogoLimits.MaxUploadEdgePixels,
        BrandingLogoLimits.MaxUploadEdgePixels,
        BrandingLogoLimits.MaxUploadPixelCount);

    private static readonly IReadOnlyList<ImageVariantRequest> Variants =
    [
        Variant(512, 90), Variant(512, 75), Variant(512, 60), Variant(384, 60), Variant(256, 60), Variant(192, 50), Variant(128, 50)
    ];

    /// <summary>Validates, scans, decodes, and re-encodes an uploaded logo.</summary>
    /// <param name="content">The upload body.</param>
    /// <param name="declaredContentType">The declared media type; only PNG, JPEG and WebP are accepted.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The content-addressed logo, or the reason the upload was refused.</returns>
    public async ValueTask<Result<BrandingLogo>> Process(Stream content, string? declaredContentType, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(content);

        var declaredType = NormalizeDeclaredType(declaredContentType);
        if (declaredType is null)
        {
            return Result.Invalid<BrandingLogo>(InvalidUploadDetail, "ContentType", "Logo must be a PNG, JPEG or WebP image.");
        }

        SeekableContent lease;
        try
        {
            lease = await SeekableContent.Create(content, BrandingLogoLimits.MaxUploadBytes, BrandingLogoLimits.SpoolMemoryThresholdBytes, ct)
                .ConfigureAwait(false);
        }
        catch (InvalidDataException)
        {
            return Result.Invalid<BrandingLogo>(
                InvalidUploadDetail,
                ContentField,
                $"Logo must be between 1 and {BrandingLogoLimits.MaxUploadBytes} bytes.");
        }

        await using var ownedLease = lease.ConfigureAwait(false);
        var upload = lease.Content;

        if (!MatchesSignature(upload, declaredType))
        {
            return Result.Invalid<BrandingLogo>(InvalidUploadDetail, ContentField, "Logo content does not match its declared image type.");
        }

        var scan = await Scan(upload, ct).ConfigureAwait(false);
        if (scan is { } refused)
        {
            return refused;
        }

        return Reencode(upload, ct);
    }

    private static string? NormalizeDeclaredType(string? declaredContentType) =>
        declaredContentType?.Trim().ToUpperInvariant() switch
        {
            "IMAGE/PNG" => "png",
            "IMAGE/JPEG" => "jpeg",
            "IMAGE/WEBP" => "webp",
            _ => null
        };

    private static bool MatchesSignature(Stream upload, string declaredType)
    {
        Span<byte> header = stackalloc byte[SignatureLength];
        upload.Position = 0;
        var read = upload.ReadAtLeast(header, SignatureLength, throwOnEndOfStream: false);
        upload.Position = 0;
        var bytes = header[..read];

        return declaredType switch
        {
            "png" => bytes.StartsWith<byte>([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
            "jpeg" => bytes.StartsWith<byte>([0xFF, 0xD8, 0xFF]),
            _ => bytes.Length == SignatureLength && bytes[..4].SequenceEqual("RIFF"u8) && bytes[8..12].SequenceEqual("WEBP"u8)
        };
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Untrusted uploads must fail closed when a scanner implementation cannot make a decision.")]
    private async ValueTask<Result<BrandingLogo>?> Scan(Stream upload, CancellationToken ct)
    {
        MalwareScanResult result;
        try
        {
            result = await scanner.Scan(upload, upload.Length, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception.ShouldHandleAsFailure(ct))
        {
            LogScannerFailure(logger, exception.GetType().Name);
            return Result.Unavailable<BrandingLogo>(ScannerUnavailableDetail);
        }
        finally
        {
            upload.Position = 0;
        }

        return result.Status switch
        {
            MalwareScanStatus.Rejected => Result.Invalid<BrandingLogo>(InvalidUploadDetail, ContentField, "Logo did not pass security scanning."),
            MalwareScanStatus.Passed or MalwareScanStatus.Disabled => null,
            _ => Result.Unavailable<BrandingLogo>(ScannerUnavailableDetail)
        };
    }

    private static Result<BrandingLogo> Reencode(Stream upload, CancellationToken ct)
    {
        ImageProcessingResult processed;
        try
        {
            processed = MagickImageProcessor.Process(new ImageProcessingRequest(upload, Variants, UploadLimits), ct);
        }
        catch (ImageProcessingException)
        {
            return Result.Invalid<BrandingLogo>(
                InvalidUploadDetail,
                ContentField,
                "Logo could not be decoded or exceeds the allowed image dimensions.");
        }

        foreach (var variant in processed.Variants)
        {
            if (variant.Content.Length <= BrandingLogoLimits.MaxEncodedBytes)
            {
                return Result.Ok(BrandingLogo.FromEncodedBytes(variant.Content));
            }
        }

        return Result.Invalid<BrandingLogo>(InvalidUploadDetail, ContentField, "Logo is too detailed to be reduced to the allowed size.");
    }

    private static ImageVariantRequest Variant(int edge, int quality) =>
        new($"logo-{edge}-{quality}", ImageOutputFormat.WebP, edge, quality, edge);

    [LoggerMessage(LogLevel.Warning, "Logo upload scanner failed. Failure type: {FailureType}.")]
    private static partial void LogScannerFailure(ILogger logger, string failureType);
}
