using ViajantesTurismo.Admin.Application;

namespace ViajantesTurismo.Admin.Testing.Fakes;

public sealed class ThrowingUnitOfWork(Exception exception) : IUnitOfWork
{
    public Task SaveEntities(CancellationToken ct) => Task.FromException(exception);
}
