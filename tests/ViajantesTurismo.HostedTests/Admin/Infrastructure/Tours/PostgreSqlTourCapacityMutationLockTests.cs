using Npgsql;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Infrastructure;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Tours;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class PostgreSqlTourCapacityMutationLockTests(PostgreSqlTestServerFixture fixture) : IClassFixture<PostgreSqlTestServerFixture>
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
