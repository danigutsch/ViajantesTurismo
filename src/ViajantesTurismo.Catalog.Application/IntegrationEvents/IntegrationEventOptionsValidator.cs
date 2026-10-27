using Microsoft.Extensions.Options;

namespace ViajantesTurismo.Catalog.Application.IntegrationEvents;

/// <summary>Validates <see cref="IntegrationEventOptions"/>, requiring a positive idempotency lock duration.</summary>
internal sealed class IntegrationEventOptionsValidator : IValidateOptions<IntegrationEventOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, IntegrationEventOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.IdempotencyLockDuration > TimeSpan.Zero
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                "Catalog integration event idempotency lock duration must be greater than zero.");
    }
}
