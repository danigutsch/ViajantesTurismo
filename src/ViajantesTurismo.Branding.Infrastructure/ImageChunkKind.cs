namespace ViajantesTurismo.Branding.Infrastructure;

/// <summary>The role of an image container chunk for the animation check.</summary>
internal enum ImageChunkKind
{
    /// <summary>A chunk the animation check does not care about.</summary>
    Other,

    /// <summary>A chunk that declares or carries animation: PNG <c>acTL</c>, WebP <c>ANIM</c> or <c>ANMF</c>.</summary>
    Animation,

    /// <summary>A PNG chunk that ends the region where an animation control chunk may appear: <c>IDAT</c> or <c>IEND</c>.</summary>
    EndOfControlRegion,

    /// <summary>The WebP extended header chunk <c>VP8X</c>, whose first payload byte holds the animation flag.</summary>
    ExtendedHeader
}
