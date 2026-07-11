namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

public sealed class AdminReadDbContextTests
{
    [Fact]
    public void SaveChanges_when_called_throws_read_only_exception()
    {
        // Arrange
        using var context = AdminReadDbContexts.Create();

        // Act
        var exception = TestAssert.Throws<InvalidOperationException>(() => context.SaveChanges());

        // Assert
        TestAssert.Equal(
            "This context is read-only. Use AdminWriteDbContext for write operations.",
            exception.Message);
    }

    [Fact]
    public async Task SaveChanges_when_called_asynchronously_throws_read_only_exception()
    {
        // Arrange
        await using var context = AdminReadDbContexts.Create();

        // Act
        var exception = await TestAssert.Throws<InvalidOperationException>(() => context.SaveChangesAsync(TestContext.Current.CancellationToken));

        // Assert
        TestAssert.Equal(
            "This context is read-only. Use AdminWriteDbContext for write operations.",
            exception.Message);
    }

}
