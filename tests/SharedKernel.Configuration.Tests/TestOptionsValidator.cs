using Microsoft.Extensions.Options;

namespace SharedKernel.Configuration.Tests;

internal sealed class TestOptionsValidator : IValidateOptions<TestOptions>
{
    public ValidateOptionsResult Validate(string? name, TestOptions options)
    {
        return ValidateOptionsResult.Success;
    }
}
