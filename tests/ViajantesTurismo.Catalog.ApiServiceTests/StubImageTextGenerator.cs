using SharedKernel.AI;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

internal sealed class StubImageTextGenerator(ImageTextGenerationResult result) : IImageTextGenerator
{
    private Exception? exception;

    public ImageTextGenerationRequest? Request { get; private set; }

    public void Throw(Exception value)
    {
        exception = value;
    }

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
