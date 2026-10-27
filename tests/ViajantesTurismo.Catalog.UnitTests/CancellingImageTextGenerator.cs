using PandaBear.AI;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Image text generator that cancels the shared source and then returns a canceled task.</summary>
/// <param name="cancellation">Source canceled when generation is requested.</param>
internal sealed class CancellingImageTextGenerator(CancellationTokenSource cancellation) : IImageTextGenerator
{
    /// <summary>Cancels the source and returns a canceled result.</summary>
    public async ValueTask<ImageTextGenerationResult> GenerateImageText(ImageTextGenerationRequest request, CancellationToken ct)
    {
        await cancellation.CancelAsync();
        return await ValueTask.FromCanceled<ImageTextGenerationResult>(ct);
    }
}
