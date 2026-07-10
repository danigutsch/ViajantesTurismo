using ViajantesTurismo.Catalog.Domain.Media;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents.Media;

namespace ViajantesTurismo.Catalog.Application.Media;
/// <summary>
/// Describes an accepted media image upload and the processing work it created.
/// </summary>
/// <param name="Image">The stored pending media image metadata.</param>
/// <param name="OriginalStoredEvent">The integration event for downstream image processing.</param>
/// <param name="ScanStatus">The malware scan status that allowed intake.</param>
public sealed record MediaImageUploadIntakeResult(
    PublicMediaImage Image,
    MediaImageOriginalStoredIntegrationEvent OriginalStoredEvent,
    MediaUploadScanStatus ScanStatus);
