using SharedKernel.HttpClients;

namespace ViajantesTurismo.Admin.Testing.Fakes.ApiClients;

public static class FakeApiClientOutcomes
{
    public static ContractCommandOutcomeDto TransportFailure() => new()
    {
        Kind = ContractCommandOutcomeKind.TransportFailure,
        Message = "HTTP transport failed."
    };

    public static ContractCommandOutcomeDto<TValue> TransportFailure<TValue>() => new()
    {
        Kind = ContractCommandOutcomeKind.TransportFailure,
        Message = "HTTP transport failed."
    };
}
