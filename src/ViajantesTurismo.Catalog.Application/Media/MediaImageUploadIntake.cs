using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using SharedKernel.ImageProcessing;
using SharedKernel.Results;
using ViajantesTurismo.Catalog.Domain.Media;
using SharedKernel.BuildingBlocks;

namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>
/// Validates, scans, stores, and records metadata for uploaded original Catalog images.
/// </summary>
public sealed class MediaImageUploadIntake(
    IMediaUploadValidator validator,
    IMediaUploadScanner scanner,
    IMediaObjectStore objectStore,
    IPublicMediaImageStore imageStore,
    IOptions<MediaUploadValidationOptions> validationOptions)
{
    private const string InvalidUploadMessage = "Media upload is invalid.";

    private const string ScannerUnavailableMessage = "Media upload scanner is unavailable.";

    private static readonly IReadOnlyList<ImageVariantRequest> ProbeVariants = [new("probe-webp", ImageOutputFormat.WebP, 1, 80, 1)];

    private readonly MediaUploadValidationOptions options = validationOptions.Value;

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
            return Result.Invalid<MediaImageUploadIntakeResult>(InvalidUploadMessage, nameof(request.MediaImageId), "Media image id is required.");
        }

        using var content = new MemoryStream();
        if (!await CopyToMemory(request.Content, content, options.MaxLengthBytes, ct).ConfigureAwait(false))
        {
            return Result.Invalid<MediaImageUploadIntakeResult>(
                InvalidUploadMessage,
                nameof(request.Length),
                $"Upload length must be between 1 and {options.MaxLengthBytes} bytes.");
        }

        var actualLength = content.Length;
        var validationErrors = validator.Validate(new MediaUploadValidationRequest(
            request.FileName,
            request.ContentType,
            actualLength,
            ReadHeaderBytes(content)));

        if (request.Length != actualLength)
        {
            validationErrors = AddValidationError(validationErrors, nameof(request.Length), "Upload length must match the received content length.");
        }

        if (validationErrors.Count > 0)
        {
            return Result.Invalid<MediaImageUploadIntakeResult>(InvalidUploadMessage, validationErrors);
        }

        content.Position = 0;
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
            return Result.Invalid<MediaImageUploadIntakeResult>(
                InvalidUploadMessage,
                nameof(request.Content),
                "Image content could not be decoded or exceeds the configured decoded image limits.");
        }

        content.Position = 0;
        var objectKey = CreateOriginalObjectKey(request.MediaImageId, request.ContentType);
        var scanResult = await Scan(objectKey, content, request.ContentType, actualLength, ct).ConfigureAwait(false);
        if (scanResult.Status is MediaUploadScanStatus.Failed)
        {
            return Result.Unavailable<MediaImageUploadIntakeResult>(scanResult.Message ?? ScannerUnavailableMessage);
        }

        if (scanResult.Status is MediaUploadScanStatus.Rejected or MediaUploadScanStatus.Pending)
        {
            return Result.Invalid<MediaImageUploadIntakeResult>(
                InvalidUploadMessage,
                nameof(scanResult.Status),
                scanResult.Message ?? "Media upload did not pass malware scanning.");
        }

        content.Position = 0;
        var checksum = ComputeChecksum(content);
        content.Position = 0;
        var stored = await objectStore.Put(
            new MediaObjectWriteRequest(objectKey, content, request.ContentType.Trim(), actualLength, checksum),
            ct).ConfigureAwait(false);
        var imageResult = PublicMediaImage.Create(
            new PublicMediaImageMetadata
            {
                Id = request.MediaImageId,
                SourceUri = stored.PublicUri,
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
            return imageResult.ConvertError<PublicMediaImage, MediaImageUploadIntakeResult>();
        }

        var image = imageResult.Value;

        await imageStore.Upsert(image, ct).ConfigureAwait(false);

        var originalStoredEvent = new MediaImageOriginalStoredIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            request.MediaImageId,
            objectKey,
            1);

        return Result.Ok(new MediaImageUploadIntakeResult(image, originalStoredEvent, scanResult.Status));
    }

    private async ValueTask<MediaUploadScanResult> Scan(string objectKey, MemoryStream content, string contentType, long length, CancellationToken ct)
    {
        try
        {
            var result = await scanner.Scan(new MediaUploadScanRequest(objectKey, content, contentType, length), ct).ConfigureAwait(false);
            content.Position = 0;
            return result;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception.ShouldHandleAsFailure(ct))
        {
            content.Position = 0;
            return new MediaUploadScanResult(MediaUploadScanStatus.Failed, ScannerUnavailableMessage);
        }
    }

    private static async ValueTask<bool> CopyToMemory(Stream source, MemoryStream destination, long maxLengthBytes, CancellationToken ct)
    {
        var buffer = new byte[81920];

        while (true)
        {
            var bytesRead = await source.ReadAsync(buffer, ct).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                destination.Position = 0;
                return true;
            }

            if (destination.Length + bytesRead > maxLengthBytes)
            {
                destination.Position = 0;
                return false;
            }

            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), ct).ConfigureAwait(false);
        }
    }

    private static ReadOnlyMemory<byte> ReadHeaderBytes(MemoryStream content)
    {
        content.Position = 0;
        var headerLength = (int)Math.Min(content.Length, 12);
        var header = new byte[headerLength];
        _ = content.Read(header);
        content.Position = 0;

        return header;
    }

    private static string ComputeChecksum(Stream content)
    {
        var hash = SHA256.HashData(content);
        return string.Create(CultureInfo.InvariantCulture, $"sha256:{Convert.ToHexString(hash).ToUpperInvariant()}");
    }

    private static string CreateOriginalObjectKey(Guid mediaImageId, string contentType)
        => string.Create(CultureInfo.InvariantCulture, $"media/{mediaImageId:N}/original{GetFileExtension(contentType)}");

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
