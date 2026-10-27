using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ViajantesTurismo.Admin.Infrastructure;

/// <summary>Database schema names and constraint checks for customers.</summary>
internal static class CustomerSchema
{
    /// <summary>Name of the unique index on customer email.</summary>
    public const string EmailUniqueIndex = "UX_CustomerContactInfo_Email";

    /// <summary>Returns whether the exception wraps a Postgres unique violation on the customer email index.</summary>
    public static bool IsEmailConflict(DbUpdateException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.InnerException is PostgresException postgresException &&
            IsEmailConflict(postgresException);
    }

    /// <summary>Returns whether the Postgres exception is a unique violation on the customer email index.</summary>
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
