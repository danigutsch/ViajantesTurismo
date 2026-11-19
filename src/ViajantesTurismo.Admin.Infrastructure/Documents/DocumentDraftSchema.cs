using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Database constraint and index names for document drafts, with the conflicts EF Core cannot report by unique rule.</summary>
internal static class DocumentDraftSchema
{
    private const string ArtifactPrimaryKey = "PK_DocumentArtifactBodies";

    /// <summary>Check constraint name enforcing booking eligibility.</summary>
    public const string BookingEligibilityConstraint = "CK_DocumentDrafts_BookingEligibility";
    /// <summary>Unique constraint name allowing one active finalized revision per lineage.</summary>
    public const string ActiveFinalizedLineageConstraint = "UQ_DocumentDrafts_ActiveFinalizedLineage";
    /// <summary>Unique index name for one lineage per booking and document type.</summary>
    public const string LineageUniqueIndex = "UX_DocumentLineages_BookingId_Type";
    /// <summary>Unique index name for one revision number per lineage.</summary>
    public const string RevisionUniqueIndex = "UX_DocumentDrafts_DocumentLineageId_Revision";
    /// <summary>Unique index name for one revision number per booking and document type.</summary>
    public const string BookingTypeRevisionUniqueIndex = "UX_DocumentDrafts_BookingId_Type_Revision";
    /// <summary>The unique rule of the lineage and revision indexes, reported when two writers create the same revision.</summary>
    public const string RevisionUniqueRule = "document.revision";

    /// <summary>Returns true when the exception wraps a booking eligibility check violation.</summary>
    public static bool IsBookingEligibilityConflict(DbUpdateException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.CheckViolation,
            ConstraintName: BookingEligibilityConstraint,
        };
    }

    /// <summary>Returns whether another writer already sealed the same revision's artifact body.</summary>
    public static bool IsDuplicateArtifactConflict(DbUpdateException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: ArtifactPrimaryKey,
        };
    }

    /// <summary>
    /// Returns true when the PostgreSQL error is a violation of the deferred one-active-finalized-revision constraint. It is created in SQL,
    /// not in the EF Core model, and PostgreSQL reports it when the transaction commits, so it cannot be reported by unique rule.
    /// </summary>
    public static bool IsActiveFinalizedLineageConflict(PostgresException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception is
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: ActiveFinalizedLineageConstraint,
        };
    }
}
