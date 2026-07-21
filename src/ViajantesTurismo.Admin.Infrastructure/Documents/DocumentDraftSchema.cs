using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

internal static class DocumentDraftSchema
{
    public const string BookingEligibilityConstraint = "CK_DocumentDrafts_BookingEligibility";
    public const string ActiveFinalizedLineageConstraint = "UQ_DocumentDrafts_ActiveFinalizedLineage";
    public const string LineageUniqueIndex = "UX_DocumentLineages_BookingId_Type";
    public const string RevisionUniqueIndex = "UX_DocumentDrafts_DocumentLineageId_Revision";

    public static bool IsBookingEligibilityConflict(DbUpdateException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.CheckViolation,
            ConstraintName: BookingEligibilityConstraint,
        };
    }

    public static bool IsRevisionConflict(DbUpdateException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.InnerException is PostgresException postgresException &&
            IsRevisionConflict(postgresException);
    }

    public static bool IsRevisionConflict(PostgresException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception is
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: LineageUniqueIndex or RevisionUniqueIndex or ActiveFinalizedLineageConstraint,
        };
    }
}
