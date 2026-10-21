using PandaBear.HttpClients;

namespace ViajantesTurismo.Admin.Testing.Fakes.ApiClients;

public static class FakeApiClientOutcomes
{
    public static HttpOutcome TransportFailure() => new()
    {
        Kind = HttpOutcomeKind.TransportFailure,
        Message = "HTTP transport failed."
    };

    public static HttpOutcome<TValue> TransportFailure<TValue>() => new()
    {
        Kind = HttpOutcomeKind.TransportFailure,
        Message = "HTTP transport failed."
    };
}
