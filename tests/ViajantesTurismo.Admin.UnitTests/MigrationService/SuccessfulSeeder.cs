using ViajantesTurismo.Admin.Application;

namespace ViajantesTurismo.Admin.UnitTests.MigrationService;

internal sealed class SuccessfulSeeder : ISeeder
{
    public bool SeedCalled { get; private set; }

    public Task Seed(CancellationToken ct)
    {
        SeedCalled = true;
        return Task.CompletedTask;
    }

    public Task ClearDatabase(CancellationToken ct) => Task.CompletedTask;
}
