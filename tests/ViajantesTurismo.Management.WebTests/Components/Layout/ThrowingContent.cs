using Microsoft.AspNetCore.Components;

namespace ViajantesTurismo.Management.WebTests.Components.Layout;

/// <summary>Page content that throws while rendering, carrying a detail that must never reach the user.</summary>
internal sealed class ThrowingContent : ComponentBase
{
    /// <summary>The exception detail that must stay out of the rendered markup.</summary>
    public const string SecretDetail = "secret connection string";

    /// <inheritdoc />
    protected override void OnInitialized() => throw new InvalidOperationException(SecretDetail);
}
