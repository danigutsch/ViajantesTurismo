using PandaBear.AI;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Image text generator stub that records the request and returns a fixed result or a queued exception.</summary>
/// <param name="result">Result returned by the generator.</param>
internal sealed class StubImageTextGenerator(ImageTextGenerationResult result) : IImageTextGenerator
{
    private Exception? exception;

    /// <summary>Gets the last request passed to the generator.</summary>
    public ImageTextGenerationRequest? Request
    {
        get; private set;
    }

    /// <summary>Makes subsequent generation calls throw the given exception.</summary>
    public void Throw(Exception value)
    {
        exception = value;
    }

    /// <summary>Records the request, then throws the queued exception or returns the fixed result.</summary>
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
