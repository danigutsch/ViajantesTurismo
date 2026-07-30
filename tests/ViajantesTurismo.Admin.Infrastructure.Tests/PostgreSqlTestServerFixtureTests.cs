using SharedKernel.Testing;

namespace ViajantesTurismo.Admin.Infrastructure.Tests;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[Trait(SharedKernelTestTraitNames.CapabilityName, TestTraits.DatabaseInitializationCapability)]
public sealed class PostgreSqlTestServerFixtureTests(PostgreSqlTestServerFixture fixture)
{
    [Fact]
    public async Task Concurrent_database_leases_share_one_server_and_isolate_data()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var firstLease = fixture.CreateDatabase(ct);
        var secondLease = fixture.CreateDatabase(ct);
        await Task.WhenAll(firstLease, secondLease);
        await using var first = await firstLease;
        await using var second = await secondLease;
        await using var firstDataSource = first.CreateDataSource("first");
        await using var secondDataSource = second.CreateDataSource("second");

        // Act
        var firstServer = await PostgreSqlTestServerFixtureTestQueries.GetServerIdentity(firstDataSource, ct);
        var secondServer = await PostgreSqlTestServerFixtureTestQueries.GetServerIdentity(secondDataSource, ct);
        await PostgreSqlTestServerFixtureTestQueries.CreateOwnedTable(firstDataSource, "first", ct);
        await PostgreSqlTestServerFixtureTestQueries.CreateOwnedTable(secondDataSource, "second", ct);
        var firstValue = await PostgreSqlTestServerFixtureTestQueries.ReadOwnedValue(firstDataSource, ct);
        var secondValue = await PostgreSqlTestServerFixtureTestQueries.ReadOwnedValue(secondDataSource, ct);

        // Assert
        firstServer.ShouldBe(secondServer);
        first.DatabaseName.ShouldNotBe(second.DatabaseName);
        firstValue.ShouldBe("first");
        secondValue.ShouldBe("second");
    }

    [Fact]
    public async Task Cancelled_database_creation_removes_the_registered_name()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var existingDatabase = await fixture.CreateDatabase(ct);
        var databaseName = $"{PostgreSqlTestDatabase.DatabaseNamePrefix}{Guid.NewGuid():N}";
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act
        Func<Task> createDatabase = () => fixture.CreateDatabase(databaseName, cancellation.Token);
        var exception = await createDatabase.ShouldThrowAssignableTo<OperationCanceledException>();
        var ownsDatabase = fixture.OwnsDatabase(databaseName);

        // Assert
        exception.ShouldNotBeNull();
        ownsDatabase.ShouldBeFalse();
    }

    [Fact]
    public async Task Disposing_one_database_lease_forces_cleanup_and_preserves_its_sibling()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var first = await fixture.CreateDatabase(ct);
        await using var sibling = await fixture.CreateDatabase(ct);
        await using var firstDataSource = first.CreateDataSource("open-connection");
        await using var openConnection = await firstDataSource.OpenConnectionAsync(ct);
        var firstDatabaseName = first.DatabaseName;

        // Act
        await first.DisposeAsync();
        var firstExists = await fixture.DatabaseExists(firstDatabaseName, ct);
        var siblingExists = await fixture.DatabaseExists(sibling.DatabaseName, ct);

        // Assert
        firstExists.ShouldBeFalse();
        siblingExists.ShouldBeTrue();
    }

    [Theory]
    [InlineData("postgres")]
    [InlineData("admin_test_not-a-guid")]
    public async Task Cleanup_rejects_database_names_not_issued_by_the_fixture(string databaseName)
    {
        // Arrange
        const string invalidConnectionString = "not-a-connection-string";

        // Act
        Func<Task> dropDatabase = () => PostgreSqlTestDatabase.DropDatabase(
            invalidConnectionString,
            databaseName);
        var exception = await dropDatabase.ShouldThrow<ArgumentException>();

        // Assert
        exception.ParamName.ShouldBe("databaseName");
    }
}
