using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using SharedKernel.BuildingBlocks;
using SharedKernel.ImageProcessing;
using SharedKernel.IO;
using SharedKernel.MalwareScanning;
using SharedKernel.Results;
using SharedKernel.UploadSanitizing.Images;

namespace ViajantesTurismo.Branding.Infrastructure;

/// <summary>
/// Turns an uploaded image into a Branding logo: validates the declared type, bounds the size, checks the structure with
/// <see cref="ImageSanitizer"/>, scans for malware, decodes, and re-encodes it so metadata is stripped and dimensions and bytes are
/// bounded. The order is: size limit, structure and animation check, declared type match, malware scan, decode and re-encode.
/// </summary>
/// <param name="scanner">The malware scanner.</param>
/// <param name="logger">The logger.</param>
public sealed partial class BrandingLogoIntake(IMalwareScanner scanner, ILogger<BrandingLogoIntake> logger)
{
    private const string InvalidUploadDetail = "Logo upload is invalid.";
    private const string ScannerUnavailableDetail = "Logo upload scanner is unavailable.";
    private const string ContentField = "Content";
    private const string MismatchDetail = "Logo content does not match its declared image type.";
    private const string UndecodableDetail = "Logo could not be decoded or exceeds the allowed image dimensions.";

    private static readonly ImageSanitizerOptions SanitizerOptions = ImageSanitizerOptions.Default with
    {
        MaxBytes = BrandingLogoLimits.MaxUploadBytes,
        MaxDimension = BrandingLogoLimits.MaxUploadEdgePixels,
        Field = ContentField,
        RejectAnimation = true
    };

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
    /// <param name="declaredContentType">
    /// The declared <c>Content-Type</c>. It is parsed as a media type: the type and subtype are compared case-insensitively with
    /// <c>image/png</c>, <c>image/jpeg</c> and <c>image/webp</c> and parameters such as <c>charset</c> are ignored. A missing,
    /// empty, malformed or list-valued (comma separated) value, or any other type, is refused with a <c>ContentType</c> error.
    /// The declared type is only a claim; the file signature must still match it.
    /// </param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The content-addressed logo, or the reason the upload was refused.</returns>
    public async ValueTask<Result<BrandingLogo>> Process(Stream content, string? declaredContentType, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (NormalizeDeclaredType(declaredContentType) is not { } declaredType)
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

        var structure = await CheckStructure(upload, declaredType, ct).ConfigureAwait(false);
        if (structure is { } malformed)
        {
            return malformed;
        }

        var scan = await Scan(upload, ct).ConfigureAwait(false);
        if (scan is { } refused)
        {
            return refused;
        }

        return Reencode(upload, ct);
    }

    private static ImageFormat? NormalizeDeclaredType(string? declaredContentType) =>
        (MediaTypeHeaderValue.TryParse(declaredContentType, out var mediaType) ? mediaType.MediaType : null)?.ToUpperInvariant() switch
        {
            "IMAGE/PNG" => ImageFormat.Png,
            "IMAGE/JPEG" => ImageFormat.Jpeg,
            "IMAGE/WEBP" => ImageFormat.Webp,
            _ => null
        };

    private static async ValueTask<Result<BrandingLogo>?> CheckStructure(Stream upload, ImageFormat declaredType, CancellationToken ct)
    {
        upload.Position = 0;
        var sanitized = await ImageSanitizer.Sanitize(upload, SanitizerOptions, ct).ConfigureAwait(false);
        upload.Position = 0;

        if (sanitized.IsFailure)
        {
            var message = sanitized.ErrorDetails?.ValidationErrors?.Values.SelectMany(static messages => messages).FirstOrDefault();
            return message switch
            {
                ImageSanitizer.NotAnImageMessage => Result.Invalid<BrandingLogo>(InvalidUploadDetail, ContentField, MismatchDetail),
                ImageSanitizer.AnimatedMessage => Result.Invalid<BrandingLogo>(
                    InvalidUploadDetail,
                    ContentField,
                    "Logo must be a still image; animated images are not accepted."),
                _ => Result.Invalid<BrandingLogo>(InvalidUploadDetail, ContentField, UndecodableDetail)
            };
        }

        return sanitized.Value.Format == declaredType
            ? null
            : Result.Invalid<BrandingLogo>(InvalidUploadDetail, ContentField, MismatchDetail);
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
            return Result.Invalid<BrandingLogo>(InvalidUploadDetail, ContentField, UndecodableDetail);
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
