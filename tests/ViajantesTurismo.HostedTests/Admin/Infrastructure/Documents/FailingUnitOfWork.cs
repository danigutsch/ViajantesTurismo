using ViajantesTurismo.Admin.Application;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Documents;

/// <summary>Unit of work whose save always fails, simulating a database outage at commit time.</summary>
internal sealed class FailingUnitOfWork : IUnitOfWork
{
    /// <summary>Throws <see cref="InvalidOperationException" /> without saving anything.</summary>
    public Task SaveEntities(CancellationToken ct) => throw new InvalidOperationException("Evidence save failed.");
}
