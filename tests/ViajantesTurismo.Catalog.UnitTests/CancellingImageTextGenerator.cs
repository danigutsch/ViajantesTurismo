using PandaBear.AI;

namespace ViajantesTurismo.Catalog.UnitTests;

internal sealed class CancellingImageTextGenerator(CancellationTokenSource cancellation) : IImageTextGenerator
{
    public async ValueTask<ImageTextGenerationResult> GenerateImageText(ImageTextGenerationRequest request, CancellationToken ct)
    {
        await cancellation.CancelAsync();
        return await ValueTask.FromCanceled<ImageTextGenerationResult>(ct);
    }
}
