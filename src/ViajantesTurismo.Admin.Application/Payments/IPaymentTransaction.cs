using PandaBear.Results;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Serializes payment mutations and rolls back rejected operations, including idempotency ownership.</summary>
public interface IPaymentTransaction
{
    /// <summary>Holds all named resource locks through the atomic commit; names must contain opaque identifiers only.</summary>
    Task<Result<Guid>> Execute(IReadOnlyList<string> lockNames, Func<Task<Result<Guid>>> mutation, CancellationToken ct);
}
