using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>
/// Releases a disposition hold. The reason is an opaque code, never free text. The releasing actor comes from the
/// authenticated principal and is not part of the request.
/// </summary>
public sealed record ReleaseDocumentHoldDto
{
    /// <summary>Gets the opaque reason code recorded with the release.</summary>
    [Required]
    [StringLength(ContractConstants.MaxDocumentHoldCodeLength)]
    [RegularExpression(ContractConstants.DocumentHoldCodePattern)]
    public required string ReasonCode
    {
        get; init;
    }
}
