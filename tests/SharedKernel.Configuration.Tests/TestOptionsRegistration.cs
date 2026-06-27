using Microsoft.Extensions.Options;

namespace SharedKernel.Configuration.Tests;

internal sealed record TestOptionsRegistration(
    IOptions<TestOptions> Options,
    TestOptions OptionsValue,
    IReadOnlyCollection<IValidateOptions<TestOptions>> Validators);
