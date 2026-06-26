using ViajantesTurismo.Admin.Application;

namespace ViajantesTurismo.Admin.UnitTests.MigrationService;

internal sealed class FailingSeeder : ISeeder
{
    public bool SeedCalled { get; private set; }

    public Task Seed(CancellationToken ct)
    {
        SeedCalled = true;
        return Task.FromException(new InvalidOperationException("boom"));
    }

    public Task ClearDatabase(CancellationToken ct) => Task.CompletedTask;
}
