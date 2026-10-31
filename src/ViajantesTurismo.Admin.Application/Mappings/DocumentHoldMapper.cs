using PandaBear.DispositionHold;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Mappings;

/// <summary>Maps disposition holds to Admin-safe read DTOs that carry opaque codes and dates only.</summary>
public static class DocumentHoldMapper
{
    /// <summary>Maps a hold to its Admin read projection as seen at <paramref name="now" />.</summary>
    public static GetDocumentHoldDto MapToGetDocumentHoldDto(Hold hold, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(hold);

        return new GetDocumentHoldDto
        {
            Id = hold.Id,
            Basis = hold.Basis.ToString(),
            PurposeCode = hold.PurposeCode,
            AuthorityReference = hold.AuthorityReference,
            PlacedBy = hold.PlacedBy,
            PlacedAt = hold.PlacedAt,
            EffectiveFrom = hold.EffectiveFrom,
            ReviewDueAt = hold.ReviewDueAt,
            ExpiresAt = hold.ExpiresAt,
            ReleasedBy = hold.ReleasedBy,
            ReleasedAt = hold.ReleasedAt,
            ReleaseReasonCode = hold.ReleaseReasonCode,
            IsRestraining = hold.RestrainsAt(now),
        };
    }
}
