using Npgsql;
using SharedKernel.Testing;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Tours;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
public sealed class PostgreSqlTourCapacityMutationLockTests(PostgreSqlTestServerFixture fixture)
{
    [Fact]
    public async Task Acquire_allows_the_same_tour_lock_to_be_reacquired_after_disposal()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await fixture.CreateDatabase(ct);
        await using var dataSource = NpgsqlDataSource.Create(database.ConnectionString);
        var sut = new PostgreSqlTourCapacityMutationLock(dataSource);
        var tourId = Guid.CreateVersion7();

        // Act
        await using (var firstLease = await sut.Acquire(tourId, ct))
        {
            firstLease.ShouldNotBeNull();
        }

        await using var secondLease = await sut.Acquire(tourId, ct);

        // Assert
        secondLease.ShouldNotBeNull();
    }
}
