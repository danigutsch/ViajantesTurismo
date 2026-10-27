using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using PandaBear.Npgsql;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Payments;

namespace ViajantesTurismo.Admin.Infrastructure.Payments;

/// <summary>Runs payment mutations in a database transaction guarded by PostgreSQL advisory locks.</summary>
/// <param name="context">The Admin write database context.</param>
internal sealed class PaymentTransaction(AdminWriteDbContext context) : IPaymentTransaction
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Execute(IReadOnlyList<string> lockNames, Func<Task<Result<Guid>>> mutation, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(lockNames);
        ArgumentNullException.ThrowIfNull(mutation);
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            context.ChangeTracker.Clear();
            await using var transaction = await context.Database.BeginTransactionAsync(ct);
            foreach (var key in lockNames.Select(PostgreSqlAdvisoryLockKey.FromUtf8Sha256BigEndian).Distinct().Order())
            {
                await PostgreSqlTransactionAdvisoryLock.Acquire((NpgsqlConnection)context.Database.GetDbConnection(), (NpgsqlTransaction)transaction.GetDbTransaction(), key, ct);
            }
            foreach (var name in lockNames.Where(name => name.StartsWith("admin:booking-finance:", StringComparison.Ordinal)).Order(StringComparer.Ordinal))
            {
                var id = Guid.ParseExact(name["admin:booking-finance:".Length..], "N");
                // Serialize against booking and price mutations as well as financial commands.
                _ = await context.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Tours\" WHERE \"Id\" = (SELECT \"TourId\" FROM \"Booking\" WHERE \"Id\" = {id}) FOR UPDATE", ct);
                _ = await context.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Booking\" WHERE \"Id\" = {id} FOR UPDATE", ct);
            }
            try
            {
                var result = await mutation();
                if (result.IsSuccess)
                {
                    await transaction.CommitAsync(ct);
                }
                else
                {
                    await transaction.RollbackAsync(ct);
                    context.ChangeTracker.Clear();
                }
                return result;
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync(ct);
                context.ChangeTracker.Clear();
                return Result.Conflict(detail: "The payment record changed. Reload and confirm the current revision.").ConvertError<Guid>();
            }
        });
    }
}
