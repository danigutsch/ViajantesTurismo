using SharedKernel.Idempotency;
using ViajantesTurismo.Admin.Application.Idempotency;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Preserves the payment resources and operation identities used by the existing database locks.</summary>
internal static class PaymentCommandLocks
{
    /// <summary>Gets the resource lock protecting one booking's financial state.</summary>
    internal static string Booking(Guid bookingId) => "admin:booking-finance:" + bookingId.ToString("N");

    /// <summary>Gets the resource lock protecting one payment account.</summary>
    internal static string Account(Guid accountId) => "admin:payment-account:" + accountId.ToString("N");

    /// <summary>Combines the requested resource locks and the operation's idempotency lock.</summary>
    internal static IReadOnlyList<string> ForOperation(string scope, IdempotencyKey key, params string[] resources) =>
        key == default ? [] : [.. resources, "admin:payment-operation:" + ApplicationRequestFingerprint.Create(scope, key.Value)];
}
