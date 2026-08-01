using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ViajantesTurismo.Admin.Infrastructure;

internal static class CustomerSchema
{
    public const string EmailUniqueIndex = "UX_CustomerContactInfo_Email";

    public static bool IsEmailConflict(DbUpdateException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.InnerException is PostgresException postgresException &&
            IsEmailConflict(postgresException);
    }

    public static bool IsEmailConflict(PostgresException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception is
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: EmailUniqueIndex,
        };
    }
}
