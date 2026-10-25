namespace ViajantesTurismo.Management.WebTests;

/// <summary>The provisioned fastText executable and model that the real-engine tests need.</summary>
internal static class FastTextEngine
{
    public const string ExecutableVariable = "MEDIA_TEST_FASTTEXT";
    public const string ModelVariable = "MEDIA_TEST_FASTTEXT_MODEL";

    /// <summary>Reads the absolute paths of the executable and the model.</summary>
    /// <returns>The executable path and the model path.</returns>
    /// <exception cref="InvalidOperationException">A variable is unset or empty.</exception>
    public static (string Executable, string Model) Require()
    {
        var executable = Environment.GetEnvironmentVariable(ExecutableVariable);
        var model = Environment.GetEnvironmentVariable(ModelVariable);
        return string.IsNullOrWhiteSpace(executable) || string.IsNullOrWhiteSpace(model)
            ? throw new InvalidOperationException(
                $"The real fastText engine tests need {ExecutableVariable} and {ModelVariable} set to the provisioned absolute paths. "
                + "Provision the engine as docs/media-accessibility-review.md describes, or leave these tests out with "
                + $"--filter-not-trait {TestTraits.RequiresName}={TestTraits.FastTextRequirement}.")
            : (executable, model);
    }
}
