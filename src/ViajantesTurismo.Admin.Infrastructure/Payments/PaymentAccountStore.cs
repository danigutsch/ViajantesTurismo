using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;

namespace ViajantesTurismo.Admin.Infrastructure.Payments;

internal sealed class PaymentAccountStore(AdminWriteDbContext context, PaymentProtection protection) : IPaymentAccountStore
{
    public async Task<PaymentAccount?> GetById(Guid id, CancellationToken ct)
    {
        var row = await context.Set<PaymentAccountRow>().AsNoTracking().SingleOrDefaultAsync(row => row.Id == id, ct);
        return row is null ? null : Decode(row);
    }

    public async Task<IReadOnlyList<PaymentAccount>> List(CancellationToken ct)
    {
        var rows = await context.Set<PaymentAccountRow>().AsNoTracking().OrderBy(row => row.Id).ToArrayAsync(ct);
        return rows.Select(Decode).ToArray();
    }

    public async Task Save(PaymentAccount account, CancellationToken ct)
    {
        var row = await context.Set<PaymentAccountRow>().SingleOrDefaultAsync(row => row.Id == account.Id, ct);
        if (row is null)
        {
            row = new PaymentAccountRow { Id = account.Id };
            context.Add(row);
        }
        row.Version = account.Version;
        row.Revision = account.Current.Number;
        row.Status = account.Status;
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(account.ExportState(), PaymentPersistenceJsonContext.Default.PaymentAccountState);
        try
        {
            row.ProtectedState = protection.Accounts.Protect(account.Id.ToString("N"), plaintext);
        }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
        foreach (var item in account.GetDomainEvents().OfType<PaymentAccountLifecycleDomainEvent>())
        {
            context.Add(new PaymentAuditRow
            {
                Id = item.Id,
                ResourceId = item.AccountId,
                Revision = item.Revision,
                ActorId = item.ActorId,
                OccurredAt = item.OccurredAt,
                Operation = "AccountLifecycle",
                Reason = item.Reason.ToString(),
                SelfApproved = item.SelfApproved
            });
        }
        account.ClearDomainEvents();
    }

    public void AuditAccess(PaymentAccount account, string actorId, DateTimeOffset now) => context.Add(new PaymentAuditRow
    {
        Id = Guid.CreateVersion7(),
        ResourceId = account.Id,
        Revision = account.Current.Number,
        ActorId = actorId,
        OccurredAt = now,
        Operation = "AccountSensitiveAccess",
        Reason = PaymentAccountReason.SensitiveAccess.ToString(),
        SelfApproved = false
    });

    private PaymentAccount Decode(PaymentAccountRow row)
    {
        var plaintext = protection.Accounts.Unprotect(row.Id.ToString("N"), row.ProtectedState);
        try
        {
            var state = JsonSerializer.Deserialize(plaintext, PaymentPersistenceJsonContext.Default.PaymentAccountState);
            var restored = PaymentAccount.Restore(state);
            if (restored.IsFailure || restored.Value.Id != row.Id || restored.Value.Version != row.Version
                || restored.Value.Current.Number != row.Revision || restored.Value.Status != row.Status)
            {
                throw new CryptographicException("Protected payment account state is invalid.");
            }
            return restored.Value;
        }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
    }
}
