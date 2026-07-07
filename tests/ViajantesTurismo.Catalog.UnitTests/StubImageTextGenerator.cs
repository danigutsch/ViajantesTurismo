using SharedKernel.AI;

namespace ViajantesTurismo.Catalog.UnitTests;

internal sealed class StubImageTextGenerator(ImageTextGenerationResult result) : IImageTextGenerator
{
    public ImageTextGenerationRequest? Request { get; private set; }

    public ValueTask<ImageTextGenerationResult> GenerateImageText(ImageTextGenerationRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Request = request;

        return ValueTask.FromResult(result);
    }
}
