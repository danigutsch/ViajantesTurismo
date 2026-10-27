using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Catalog.ApiService;

/// <summary>Multipart form payload for a media image upload.</summary>
internal readonly record struct MediaImageUploadFormDto
{
    /// <summary>The uploaded image file.</summary>
    [Required]
    [FromForm(Name = "file")]
    public required IFormFile File
    {
        get; init;
    }

    /// <summary>Required alternative text describing the image.</summary>
    [FromForm(Name = "altText")]
    [Required]
    public string? AltText
    {
        get; init;
    }

    /// <summary>Optional caption.</summary>
    [FromForm(Name = "caption")]
    public string? Caption
    {
        get; init;
    }

    /// <summary>Optional attribution of the image author.</summary>
    [FromForm(Name = "attribution")]
    public string? Attribution
    {
        get; init;
    }

    /// <summary>Optional copyright notice.</summary>
    [FromForm(Name = "copyright")]
    public string? Copyright
    {
        get; init;
    }
}
