using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Payments;

/// <summary>Records the actual PostgreSQL reads for payment audit rows.</summary>
internal sealed class PaymentInstructionReviewQueryProbe : DbCommandInterceptor
{
    private readonly ConcurrentQueue<string> _auditQueries = new();

    /// <summary>Gets the audit reads observed so far.</summary>
    internal IReadOnlyList<string> AuditQueries => [.. _auditQueries];

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
        CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        if (command.CommandText.Contains("FROM \"PaymentAudits\"", StringComparison.Ordinal))
        {
            _auditQueries.Enqueue(command.CommandText);
        }

        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }
}
