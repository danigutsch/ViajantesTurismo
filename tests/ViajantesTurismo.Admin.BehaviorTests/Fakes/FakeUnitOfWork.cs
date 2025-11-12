using ViajantesTurismo.Admin.Application;

namespace ViajantesTurismo.Admin.BehaviorTests.Fakes;

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public Task SaveEntities(CancellationToken ct = default) => Task.FromResult(true);
}
