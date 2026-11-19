using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using PandaBear.ObjectStorage;
using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PandaBear.ImageProcessing;
using PandaBear.IO;
using PandaBear.Messaging.IntegrationEvents;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents.Media;
using PandaBear.Results;
using ViajantesTurismo.Catalog.Domain.Media;
using ViajantesTurismo.Catalog.Domain;
using PandaBear.BuildingBlocks;
using PandaBear.UploadSanitizing.Images;

namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>
/// Validates, scans, stores, and records metadata for uploaded original Catalog images.
/// </summary>
public sealed partial class MediaImageUploadIntake(
    IMediaUploadValidator validator,
    IMediaUploadScanner scanner,
    IObjectStore objectStore,
    IPublicMediaImageStore imageStore,
    IIntegrationEventOutbox outbox,
    IOptions<MediaUploadValidationOptions> validationOptions,
    ILogger<MediaImageUploadIntake> logger,
    TimeProvider timeProvider)
{
    private const string InvalidUploadMessage = "Media upload is invalid.";

    private const string ScannerUnavailableMessage = "Media upload scanner is unavailable.";

    private const string ScanRejectedMessage = "Media upload did not pass security scanning.";

    private const string GalleryPlacementConflictMessage = "The tour gallery changed while this upload was processing. Retry the upload.";

    /// <summary>
    /// Largest content a non-seekable upload may hold in memory before it spools to a private temporary file. A seekable upload is
    /// read in place and never reaches this limit. One mebibyte keeps a slow or non-seekable request well below the five mebibyte
    /// upload maximum, so concurrent requests do not each pin a large-object-heap buffer.
    /// </summary>
    internal const int SpoolMemoryThresholdBytes = 1024 * 1024;

    private static readonly IReadOnlyList<ImageVariantRequest> ProbeVariants = [new("probe-webp", ImageOutputFormat.WebP, 1, 80, 1)];

    private const string ContentField = "Content";

    private const string SignatureMismatchMessage = "File signature does not match the allowed content type.";

    private const int SignaturePrefixBytes = 12;

    private const int MinimumAvifFileTypeBytes = 16;

    private readonly MediaUploadValidationOptions options = validationOptions.Value;

    private ImageSanitizerOptions SanitizerOptions => new()
    {
        AllowAvif = true,
        MaxBytes = options.MaxLengthBytes,
        Field = ContentField,
    };

    /// <summary>
    /// Accepts an uploaded original image when validation and scanning allow it.
    /// </summary>
    /// <param name="request">The upload intake request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The accepted metadata and processing event.</returns>
    public async ValueTask<Result<MediaImageUploadIntakeResult>> Accept(MediaImageUploadIntakeRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Content);
        ArgumentNullException.ThrowIfNull(request.TourLinks);

        if (request.MediaImageId == Guid.Empty)
        {
            MediaImageUploadMetrics.RecordAttempt(MediaImageUploadMetrics.Outcome.InvalidFile);
            return Result.Invalid<MediaImageUploadIntakeResult>(InvalidUploadMessage, nameof(request.MediaImageId), "Media image id is required.");
        }

        SeekableContent lease;
        try
        {
            lease = await SeekableContent.Create(request.Content, options.MaxLengthBytes, SpoolMemoryThresholdBytes, ct).ConfigureAwait(false);
        }
        catch (InvalidDataException)
        {
            MediaImageUploadMetrics.RecordAttempt(MediaImageUploadMetrics.Outcome.InvalidFile);
            return Result.Invalid<MediaImageUploadIntakeResult>(
                InvalidUploadMessage,
                nameof(request.Length),
                $"Upload length must be between 1 and {options.MaxLengthBytes} bytes.");
        }

        await using var ownedLease = lease.ConfigureAwait(false);
        var content = lease.Content;
        var actualLength = content.Length;
        var validationErrors = validator.Validate(new MediaUploadValidationRequest(
            request.FileName,
            request.ContentType,
            actualLength));

        if (request.Length != actualLength)
        {
            validationErrors = AddValidationError(validationErrors, nameof(request.Length), "Upload length must match the received content length.");
        }

        if (validationErrors.Count > 0)
        {
            MediaImageUploadMetrics.RecordAttempt(MediaImageUploadMetrics.Outcome.InvalidFile);
            return Result.Invalid<MediaImageUploadIntakeResult>(InvalidUploadMessage, validationErrors);
        }

        var canonicalContentType = GetCanonicalContentType(request.ContentType);
        if (!await HasExpectedSignature(content, canonicalContentType, actualLength, ct).ConfigureAwait(false))
        {
            MediaImageUploadMetrics.RecordAttempt(MediaImageUploadMetrics.Outcome.InvalidFile);
            return Result.Invalid<MediaImageUploadIntakeResult>(InvalidUploadMessage, ContentField, SignatureMismatchMessage);
        }

        var objectKey = CreateOriginalObjectKey(request.MediaImageId, Guid.CreateVersion7(), canonicalContentType);
        var scanResult = await Scan(objectKey, content, canonicalContentType, actualLength, ct).ConfigureAwait(false);
        if (scanResult.Status is MediaUploadScanStatus.Failed)
        {
            LogScanDiagnostic(scanResult.Status);
            MediaImageUploadMetrics.RecordAttempt(MediaImageUploadMetrics.Outcome.ScannerUnavailable);
            return Result.Unavailable<MediaImageUploadIntakeResult>(ScannerUnavailableMessage);
        }

        if (scanResult.Status is MediaUploadScanStatus.Rejected or MediaUploadScanStatus.Pending)
        {
            LogScanDiagnostic(scanResult.Status);
            MediaImageUploadMetrics.RecordAttempt(MediaImageUploadMetrics.Outcome.ScanRejected);
            return Result.Invalid<MediaImageUploadIntakeResult>(
                InvalidUploadMessage,
                nameof(scanResult.Status),
                ScanRejectedMessage);
        }

        var buffer = ArrayPool<byte>.Shared.Rent((int)actualLength);
        try
        {
            await content.ReadExactlyAsync(buffer.AsMemory(0, (int)actualLength), ct).ConfigureAwait(false);
            var sanitized = ImageSanitizer.SanitizeInPlace(buffer.AsMemory(0, (int)actualLength), SanitizerOptions);
            if (IsWrongFormat(sanitized, canonicalContentType))
            {
                MediaImageUploadMetrics.RecordAttempt(MediaImageUploadMetrics.Outcome.InvalidFile);
                return Result.Invalid<MediaImageUploadIntakeResult>(InvalidUploadMessage, ContentField, SignatureMismatchMessage);
            }

            if (sanitized.IsFailure)
            {
                MediaImageUploadMetrics.RecordAttempt(MediaImageUploadMetrics.Outcome.InvalidFile);
                return sanitized.ConvertError<SanitizedImage, MediaImageUploadIntakeResult>();
            }

            _ = MemoryMarshal.TryGetArray(sanitized.Value.Bytes, out var cleanedBytes);
            using var cleaned = new MemoryStream(buffer, cleanedBytes.Offset, cleanedBytes.Count, writable: false);
            content = cleaned;
            actualLength = cleaned.Length;
            ImageProcessingResult decoded;
            try
            {
                decoded = MagickImageProcessor.Process(
                    new ImageProcessingRequest(
                        content,
                        ProbeVariants,
                        new ImageProcessingLimits(options.MaxDecodedWidth, options.MaxDecodedHeight, options.MaxDecodedPixelCount)),
                    ct);
            }
            catch (ImageProcessingException)
            {
                MediaImageUploadMetrics.RecordAttempt(MediaImageUploadMetrics.Outcome.InvalidFile);
                return Result.Invalid<MediaImageUploadIntakeResult>(
                    InvalidUploadMessage,
                    nameof(request.Content),
                    "Image content could not be decoded or exceeds the configured decoded image limits.");
            }

            content.Position = 0;
            var checksum = ComputeChecksum(content);
            content.Position = 0;
            var stored = await StoreOriginal(
                ObjectWriteRequest.Create(objectKey, content, canonicalContentType, actualLength, checksum),
                ct).ConfigureAwait(false);
            var imageResult = PublicMediaImage.Create(
                new PublicMediaImageMetadata
                {
                    Id = request.MediaImageId,
                    SourceObjectKey = stored.ObjectKey,
                    Checksum = checksum,
                    ContentType = stored.ContentType,
                    FileSizeBytes = stored.Length,
                    Dimensions = new MediaImageDimensions(decoded.Width, decoded.Height),
                    ProcessingStatus = MediaImageProcessingStatus.Pending,
                    AltText = request.AltText,
                    Caption = request.Caption,
                    Attribution = request.Attribution,
                    Copyright = request.Copyright,
                },
                [],
                request.Tags ?? [],
                request.TourLinks);
            if (imageResult.IsFailure)
            {
                await MediaObjectCompensation.Delete(objectStore, objectKey.Value).ConfigureAwait(false);

                MediaImageUploadMetrics.RecordAttempt(MediaImageUploadMetrics.Outcome.InvalidFile);
                return imageResult.ConvertError<PublicMediaImage, MediaImageUploadIntakeResult>();
            }

            var image = imageResult.Value;

            var originalStoredEvent = new MediaImageOriginalStoredIntegrationEvent(
                Guid.CreateVersion7(),
                timeProvider.GetUtcNow(),
                request.MediaImageId,
                objectKey.Value,
                1);

            try
            {
                await StoreImageEventAndCompensateObject(image, originalStoredEvent, objectKey, ct).ConfigureAwait(false);
            }
            catch (MediaGalleryPlacementConflictException)
            {
                MediaImageUploadMetrics.RecordAttempt(MediaImageUploadMetrics.Outcome.GalleryConflict);
                return Result.Conflict<MediaImageUploadIntakeResult>(GalleryPlacementConflictMessage);
            }

            MediaImageUploadMetrics.RecordAttempt(MediaImageUploadMetrics.Outcome.Accepted);
            return Result.Ok(new MediaImageUploadIntakeResult(image, originalStoredEvent, scanResult.Status));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static async ValueTask<bool> HasExpectedSignature(Stream content, string contentType, long length, CancellationToken ct)
    {
        var signature = new byte[SignaturePrefixBytes];
        var bytesRead = await content.ReadAtLeastAsync(
            signature, signature.Length, throwOnEndOfStream: false, cancellationToken: ct).ConfigureAwait(false);
        content.Position = 0;
        var prefix = signature.AsSpan(0, bytesRead);

        return contentType switch
        {
            "image/jpeg" => prefix.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF]),
            "image/png" => prefix.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
            "image/webp" => prefix.Length == SignaturePrefixBytes && prefix[..4].SequenceEqual("RIFF"u8) && prefix[8..].SequenceEqual("WEBP"u8),
            // AVIF compatible brands can follow the fixed prefix. The sanitizer checks them after scanning.
            "image/avif" => length >= MinimumAvifFileTypeBytes && prefix.Length == SignaturePrefixBytes && prefix[4..8].SequenceEqual("ftyp"u8),
            _ => false
        };
    }

    private async ValueTask<MediaUploadScanResult> Scan(ObjectStorageKey objectKey, Stream content, string contentType, long length, CancellationToken ct)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            var result = await scanner.Scan(new MediaUploadScanRequest(objectKey.Value, content, contentType, length), ct).ConfigureAwait(false);
            content.Position = 0;
            return result;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception.ShouldHandleAsFailure(ct))
        {
            LogScannerFailure(logger, exception.GetType().Name);
            content.Position = 0;
            return new MediaUploadScanResult(MediaUploadScanStatus.Failed, ScannerUnavailableMessage);
        }
        finally
        {
            MediaImageUploadMetrics.RecordStage(MediaImageUploadMetrics.Stage.Scan, started);
        }
    }

    private async ValueTask<ObjectWriteResult> StoreOriginal(ObjectWriteRequest request, CancellationToken ct)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            return await objectStore.Put(request, ct).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception.ShouldHandleAsFailure(ct))
        {
            MediaImageUploadMetrics.RecordAttempt(MediaImageUploadMetrics.Outcome.StorageFailed);
            throw;
        }
        finally
        {
            MediaImageUploadMetrics.RecordStage(MediaImageUploadMetrics.Stage.Store, started);
        }
    }

    private async ValueTask StoreImageEventAndCompensateObject(
        PublicMediaImage image,
        MediaImageOriginalStoredIntegrationEvent originalStoredEvent,
        ObjectStorageKey objectKey,
        CancellationToken ct)
    {
        try
        {
            await Compensation.CompleteOrCompensate(
                async ct =>
                {
                    await outbox.Enqueue(originalStoredEvent, ct).ConfigureAwait(false);
                    await imageStore.Upsert(image, ct).ConfigureAwait(false);
                },
                _ => MediaObjectCompensation.Delete(objectStore, objectKey.Value),
                ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            await MediaObjectCompensation.Delete(objectStore, objectKey.Value).ConfigureAwait(false);

            throw;
        }
    }

    /// <summary>
    /// Checks the actual format reported by the sanitizer, including the AVIF brands beyond the bounded prefix.
    /// </summary>
    private static bool IsWrongFormat(Result<SanitizedImage> sanitized, string canonicalContentType) => sanitized.IsSuccess
        ? !string.Equals(sanitized.Value.ContentType, canonicalContentType, StringComparison.Ordinal)
        : sanitized.ErrorDetails?.ValidationErrors?.Values.Any(messages => messages.Contains(ImageSanitizer.NotAnImageMessage)) == true;

    private static string ComputeChecksum(Stream content)
    {
        var hash = SHA256.HashData(content);
        return string.Create(CultureInfo.InvariantCulture, $"sha256:{Convert.ToHexString(hash).ToUpperInvariant()}");
    }

    private void LogScanDiagnostic(MediaUploadScanStatus scanStatus) => LogScanDiagnostic(logger, scanStatus);

    [LoggerMessage(LogLevel.Warning, "Media upload scanner failed. Failure type: {FailureType}.")]
    private static partial void LogScannerFailure(ILogger logger, string failureType);

    [LoggerMessage(LogLevel.Warning, "Media upload scanner returned {ScanStatus}.")]
    private static partial void LogScanDiagnostic(ILogger logger, MediaUploadScanStatus scanStatus);

    private static ObjectStorageKey CreateOriginalObjectKey(Guid mediaImageId, Guid uploadAttemptId, string contentType)
        => ObjectStorageKey.From(
            string.Create(CultureInfo.InvariantCulture, $"media/{mediaImageId:N}/original-{uploadAttemptId:N}{GetFileExtension(contentType)}"),
            CatalogDomainLimits.MaxMediaObjectKeyLength);

    /// <summary>
    /// Maps a type allowed by the upload validator to its canonical media type.
    /// </summary>
    private static string GetCanonicalContentType(string contentType) => contentType.Trim().ToUpperInvariant() switch
    {
        "IMAGE/AVIF" => "image/avif",
        "IMAGE/JPEG" => "image/jpeg",
        "IMAGE/PNG" => "image/png",
        "IMAGE/WEBP" => "image/webp",
        _ => throw new ArgumentOutOfRangeException(nameof(contentType), contentType, "Unsupported image content type.")
    };

    private static string GetFileExtension(string contentType) => contentType.Trim().ToUpperInvariant() switch
    {
        "IMAGE/AVIF" => ".avif",
        "IMAGE/JPEG" => ".jpg",
        "IMAGE/PNG" => ".png",
        "IMAGE/WEBP" => ".webp",
        _ => throw new ArgumentOutOfRangeException(nameof(contentType), contentType, "Unsupported image content type.")
    };

    private static Dictionary<string, string[]> AddValidationError(
        IReadOnlyDictionary<string, string[]> errors,
        string field,
        string message)
    {
        var updated = new Dictionary<string, string[]>(errors, StringComparer.Ordinal)
        {
            [field] = errors.TryGetValue(field, out var existing) ? [.. existing, message] : [message]
        };

        return updated;
    }
}
