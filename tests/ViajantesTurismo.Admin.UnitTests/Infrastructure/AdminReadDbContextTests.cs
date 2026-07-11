namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

public sealed class AdminReadDbContextTests
{
    [Fact]
    public void SaveChanges_when_called_throws_read_only_exception()
    {
        // Arrange
        using var context = AdminReadDbContexts.Create();

        // Act
        var exception = ((Func<object?>)(() => context.SaveChanges())).ShouldThrow<InvalidOperationException>();

        // Assert
        (exception.Message).ShouldBe("This context is read-only. Use AdminWriteDbContext for write operations.");
    }

    [Fact]
    public async Task SaveChanges_when_called_asynchronously_throws_read_only_exception()
    {
        // Arrange
        await using var context = AdminReadDbContexts.Create();

        // Act
        var exception = await ((Func<Task>)(() => context.SaveChangesAsync(TestContext.Current.CancellationToken))).ShouldThrow<InvalidOperationException>();

        // Assert
        (exception.Message).ShouldBe("This context is read-only. Use AdminWriteDbContext for write operations.");
    }

}
