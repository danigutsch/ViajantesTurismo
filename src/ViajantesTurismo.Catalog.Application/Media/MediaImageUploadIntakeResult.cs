using ViajantesTurismo.Catalog.Domain.Media;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents.Media;

namespace ViajantesTurismo.Catalog.Application.Media;
/// <summary>
/// Describes an accepted media image upload.
/// </summary>
/// <param name="Image">The stored media image metadata.</param>
/// <param name="OriginalStoredEvent">The downstream processing event when the upload creates an image.</param>
/// <param name="ScanStatus">The malware scan status that allowed intake.</param>
public sealed record MediaImageUploadIntakeResult(
    PublicMediaImage Image,
    MediaImageOriginalStoredIntegrationEvent? OriginalStoredEvent,
    MediaUploadScanStatus ScanStatus);
