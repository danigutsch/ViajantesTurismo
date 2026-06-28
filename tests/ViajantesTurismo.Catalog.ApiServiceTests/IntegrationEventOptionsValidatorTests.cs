using ViajantesTurismo.Catalog.Application.IntegrationEvents;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

public sealed class IntegrationEventOptionsValidatorTests
{
    [Fact]
    public void Validate_accepts_the_default_options()
    {
        // Arrange
        var validator = new IntegrationEventOptionsValidator();

        // Act
        var result = validator.Validate(null, new IntegrationEventOptions());

        // Assert
        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_rejects_non_positive_idempotency_lock_duration(int seconds)
    {
        // Arrange
        var validator = new IntegrationEventOptionsValidator();
        var options = new IntegrationEventOptions
        {
            IdempotencyLockDuration = TimeSpan.FromSeconds(seconds)
        };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failures);
        Assert.Contains(
            "Catalog integration event idempotency lock duration must be greater than zero.",
            result.Failures,
            StringComparer.Ordinal);
    }

    [Fact]
    public void AddCatalogApplication_binds_idempotency_lock_duration_from_configuration()
    {
        // Arrange
        var configuredDuration = TimeSpan.FromMinutes(2);

        // Act
        var options = Infrastructure.IntegrationEventOptionsTestServices.GetConfiguredOptions(configuredDuration);

        // Assert
        Assert.Equal(configuredDuration, options.IdempotencyLockDuration);
    }
}
