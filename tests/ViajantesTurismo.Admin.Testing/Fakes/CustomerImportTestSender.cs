using PandaBear.Mediator;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Application.Import;

namespace ViajantesTurismo.Admin.Testing.Fakes;

/// <summary>Dispatches customer import commands to the callback supplied by a test scenario.</summary>
/// <param name="handle">Handles the import command within the test scenario.</param>
public sealed class CustomerImportTestSender(
    Func<CustomerImportCommand, CancellationToken, ValueTask<Result<ImportResult>>> handle) : ISender
{
    /// <inheritdoc />
    public async ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct)
    {
        if (request is not CustomerImportCommand command)
        {
            throw new NotSupportedException("This test sender dispatches only customer import commands.");
        }

        var result = await handle(command, ct);
        return (TResponse)(object)result;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<TResponse> Send<TResponse>(IStreamRequest<TResponse> request, CancellationToken ct) =>
        throw new NotSupportedException("This test sender dispatches only customer import commands.");
}
