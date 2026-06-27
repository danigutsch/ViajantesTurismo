using Microsoft.Extensions.Options;
using ViajantesTurismo.Catalog.Application.IntegrationEvents;

namespace ViajantesTurismo.Catalog.ApiService;

internal sealed class CatalogIntegrationEventOptionsValidator : IValidateOptions<CatalogIntegrationEventOptions>
{
    public ValidateOptionsResult Validate(string? name, CatalogIntegrationEventOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.IdempotencyLockDuration <= TimeSpan.Zero)
        {
            return ValidateOptionsResult.Fail(
                "Catalog integration event idempotency lock duration must be greater than zero.");
        }

        return ValidateOptionsResult.Success;
    }
}
