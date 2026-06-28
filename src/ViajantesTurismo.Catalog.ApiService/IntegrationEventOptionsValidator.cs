using Microsoft.Extensions.Options;
using ViajantesTurismo.Catalog.Application.IntegrationEvents;

namespace ViajantesTurismo.Catalog.ApiService;

internal sealed class IntegrationEventOptionsValidator : IValidateOptions<IntegrationEventOptions>
{
    public ValidateOptionsResult Validate(string? name, IntegrationEventOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.IdempotencyLockDuration > TimeSpan.Zero
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                "Catalog integration event idempotency lock duration must be greater than zero.");
    }
}
