using PandaBear.AI;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

/// <summary>Image text generator stub that returns a fixed result, records the request, and can be told to throw.</summary>
/// <param name="result">The result returned for every generation call.</param>
internal sealed class StubImageTextGenerator(ImageTextGenerationResult result) : IImageTextGenerator
{
    private Exception? exception;

    /// <summary>Gets the last request received, or null when none was made.</summary>
    public ImageTextGenerationRequest? Request
    {
        get; private set;
    }

    /// <summary>Makes subsequent generation calls throw the given exception.</summary>
    public void Throw(Exception value)
    {
        exception = value;
    }

    /// <summary>Records the request, then throws the configured exception or returns the fixed result.</summary>
    public ValueTask<ImageTextGenerationResult> GenerateImageText(ImageTextGenerationRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Request = request;
        if (exception is not null)
        {
            throw exception;
        }

        return ValueTask.FromResult(result);
    }
}
