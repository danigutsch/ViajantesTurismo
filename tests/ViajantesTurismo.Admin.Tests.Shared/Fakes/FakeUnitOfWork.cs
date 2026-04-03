using ViajantesTurismo.Admin.Application;

namespace ViajantesTurismo.Admin.Tests.Shared.Fakes;

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveEntitiesCallCount { get; private set; }

    public Task SaveEntities(CancellationToken ct)
    {
        SaveEntitiesCallCount++;
        return Task.CompletedTask;
    }
}
