using ViajantesTurismo.Admin.Application;

namespace ViajantesTurismo.Admin.Tests.Shared.Fakes;

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public Task SaveEntities(CancellationToken ct = default) => Task.CompletedTask;
}
