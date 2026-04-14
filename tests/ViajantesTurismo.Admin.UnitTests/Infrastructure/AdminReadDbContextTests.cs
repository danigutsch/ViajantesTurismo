using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Infrastructure;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

public sealed class AdminReadDbContextTests
{
    [Fact]
    public void SaveChanges_When_Called_Throws_Read_Only_Exception()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => context.SaveChanges());

        // Assert
        Assert.Equal(
            "This context is read-only. Use AdminWriteDbContext for write operations.",
            exception.Message);
    }

    [Fact]
    public async Task SaveChanges_When_Called_Asynchronously_Throws_Read_Only_Exception()
    {
        // Arrange
        await using var context = CreateContext();

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync(TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal(
            "This context is read-only. Use AdminWriteDbContext for write operations.",
            exception.Message);
    }

    private static AdminReadDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AdminReadDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AdminReadDbContext(options);
    }
}
