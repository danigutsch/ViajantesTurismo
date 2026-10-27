using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Database constraint and index names for document drafts, with PostgreSQL conflict detection.</summary>
internal static class DocumentDraftSchema
{
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

    /// <summary>Returns true when the exception wraps a unique violation on a revision or lineage constraint.</summary>
    public static bool IsRevisionConflict(DbUpdateException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.InnerException is PostgresException postgresException &&
            IsRevisionConflict(postgresException);
    }

    /// <summary>Returns true when the PostgreSQL error is a unique violation on a revision or lineage constraint.</summary>
    public static bool IsRevisionConflict(PostgresException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception is
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: LineageUniqueIndex or RevisionUniqueIndex or BookingTypeRevisionUniqueIndex or ActiveFinalizedLineageConstraint,
        };
    }
}
