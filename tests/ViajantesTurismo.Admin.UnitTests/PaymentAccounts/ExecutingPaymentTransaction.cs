using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Payments;

namespace ViajantesTurismo.Admin.UnitTests.PaymentAccounts;

/// <summary>Executes a payment mutation in process and captures its declared lock identities.</summary>
internal sealed class ExecutingPaymentTransaction : IPaymentTransaction
{
    /// <summary>Gets how many payment transactions were requested.</summary>
    public int Calls
    {
        get; private set;
    }

    /// <summary>Gets the lock identities of the most recent transaction.</summary>
    public IReadOnlyList<string> LockNames { get; private set; } = [];

    /// <inheritdoc />
    public Task<Result<Guid>> Execute(IReadOnlyList<string> lockNames, Func<Task<Result<Guid>>> mutation, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Calls++;
        LockNames = lockNames;
        return mutation();
    }
}
