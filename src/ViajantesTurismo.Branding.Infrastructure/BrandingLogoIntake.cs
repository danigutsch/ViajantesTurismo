using System.Buffers.Binary;
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
/// Animated WebP and APNG uploads are refused by a bounded chunk walk before anything is decoded.
/// </summary>
/// <param name="scanner">The malware scanner.</param>
/// <param name="logger">The logger.</param>
public sealed partial class BrandingLogoIntake(IMalwareScanner scanner, ILogger<BrandingLogoIntake> logger)
{
    private const string InvalidUploadDetail = "Logo upload is invalid.";
    private const string ScannerUnavailableDetail = "Logo upload scanner is unavailable.";
    private const string ContentField = "Content";
    private const int SignatureLength = 12;
    private const int PngSignatureLength = 8;
    private const int PngChunkHeaderLength = 8;
    private const int PngChunkCrcLength = 4;
    private const int WebPChunkHeaderLength = 8;
    private const int WebPAnimationFlag = 0x02;

    private static readonly ImageProcessingLimits UploadLimits = new(
        BrandingLogoLimits.MaxUploadEdgePixels,
        BrandingLogoLimits.MaxUploadEdgePixels,
        BrandingLogoLimits.MaxUploadPixelCount);

    private static readonly IReadOnlyList<ImageVariantRequest> Variants =
    [
        Variant(512, 90), Variant(512, 75), Variant(512, 60), Variant(384, 60), Variant(256, 60), Variant(192, 50), Variant(128, 50)
    ];

    /// <summary>Validates, scans, decodes, and re-encodes an uploaded logo; animated images are refused.</summary>
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

        if (IsAnimated(upload, declaredType))
        {
            return Result.Invalid<BrandingLogo>(InvalidUploadDetail, ContentField, "Logo must be a still image; animated images are not accepted.");
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

    private static bool IsAnimated(Stream upload, string declaredType)
    {
        try
        {
            return declaredType switch
            {
                "png" => HasAnimationControlChunk(upload),
                "webp" => HasWebPAnimation(upload),
                _ => false
            };
        }
        finally
        {
            upload.Position = 0;
        }
    }

    private static bool HasAnimationControlChunk(Stream upload)
    {
        Span<byte> header = stackalloc byte[PngChunkHeaderLength];
        upload.Position = PngSignatureLength;
        while (upload.ReadAtLeast(header, PngChunkHeaderLength, throwOnEndOfStream: false) == PngChunkHeaderLength)
        {
            var type = header[4..];
            if (type.SequenceEqual("acTL"u8))
            {
                return true;
            }

            if (type.SequenceEqual("IDAT"u8) || type.SequenceEqual("IEND"u8))
            {
                return false;
            }

            upload.Position += BinaryPrimitives.ReadUInt32BigEndian(header) + (long)PngChunkCrcLength;
        }

        return false;
    }

    private static bool HasWebPAnimation(Stream upload)
    {
        Span<byte> header = stackalloc byte[WebPChunkHeaderLength];
        upload.Position = SignatureLength;
        while (upload.ReadAtLeast(header, WebPChunkHeaderLength, throwOnEndOfStream: false) == WebPChunkHeaderLength)
        {
            var type = header[..4];
            if (type.SequenceEqual("ANIM"u8) || type.SequenceEqual("ANMF"u8))
            {
                return true;
            }

            var size = BinaryPrimitives.ReadUInt32LittleEndian(header[4..]);
            var payloadStart = upload.Position;
            if (type.SequenceEqual("VP8X"u8) && size > 0 && (upload.ReadByte() & WebPAnimationFlag) != 0)
            {
                return true;
            }

            upload.Position = payloadStart + size + (size & 1);
        }

        return false;
    }

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
