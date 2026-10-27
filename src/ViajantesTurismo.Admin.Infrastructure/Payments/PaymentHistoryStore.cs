using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Domain.Payments;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Infrastructure.Payments;

/// <summary>Persists booking financial histories as protected rows.</summary>
/// <param name="context">The write database context.</param>
/// <param name="protection">Protects and unprotects ledger state.</param>
internal sealed class PaymentHistoryStore(AdminWriteDbContext context, PaymentProtection protection) : IPaymentHistoryStore
{
    /// <summary>Writes the booking financial history as a protected row and stages audit rows for its events.</summary>
    public async Task Save(Tour tour, Guid bookingId, CancellationToken ct)
    {
        var history = tour.Bookings.Single(booking => booking.Id == bookingId).FinancialHistory
            ?? throw new InvalidOperationException("Financial history is required.");
        var row = await context.Set<PaymentHistoryRow>().SingleOrDefaultAsync(row => row.BookingId == bookingId, ct);
        if (row is null)
        {
            row = new PaymentHistoryRow { BookingId = bookingId };
            context.Add(row);
        }
        row.Version = history.Version;
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(history.ExportState(), PaymentPersistenceJsonContext.Default.BookingFinancialState);
        try
        {
            row.ProtectedState = protection.Ledgers.Protect(bookingId.ToString("N"), plaintext);
        }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
        foreach (var item in history.GetDomainEvents().OfType<FinancialLifecycleDomainEvent>())
        {
            context.Add(new PaymentAuditRow
            {
                Id = item.Id,
                ResourceId = item.BookingId,
                TargetId = item.TargetId,
                Revision = item.Version,
                ActorId = item.ActorId,
                OccurredAt = item.OccurredAt,
                Operation = item.Operation,
                Reason = item.Reason.ToString(),
                SelfApproved = item.SelfApproved
            });
        }
        history.ClearDomainEvents();
    }

    /// <summary>Restores financial histories onto the given bookings from their stored rows.</summary>
    internal async Task Attach(IReadOnlyCollection<Booking> bookings, CancellationToken ct)
    {
        var ids = bookings.Select(booking => booking.Id).ToArray();
        var rows = await context.Set<PaymentHistoryRow>().AsNoTracking().Where(row => ids.Contains(row.BookingId)).ToArrayAsync(ct);
        foreach (var row in rows)
        {
            bookings.Single(booking => booking.Id == row.BookingId).RestoreFinancialHistory(Decode(row));
        }
    }

    /// <summary>Decrypts and validates a history row, throwing when it does not match the row.</summary>
    internal BookingFinancialHistory Decode(PaymentHistoryRow row)
    {
        var plaintext = protection.Ledgers.Unprotect(row.BookingId.ToString("N"), row.ProtectedState);
        try
        {
            var state = JsonSerializer.Deserialize(plaintext, PaymentPersistenceJsonContext.Default.BookingFinancialState);
            var restored = BookingFinancialHistory.Restore(state);
            if (restored.IsFailure || restored.Value.BookingId != row.BookingId || restored.Value.Version != row.Version)
            {
                throw new CryptographicException("Protected financial history is invalid.");
            }
            return restored.Value;
        }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
    }
}
