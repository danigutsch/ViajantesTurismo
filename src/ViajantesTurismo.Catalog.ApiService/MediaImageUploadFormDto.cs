using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Catalog.ApiService;

internal readonly record struct MediaImageUploadFormDto
{
    [Required]
    [FromForm(Name = "file")]
    public required IFormFile File { get; init; }

    [FromForm(Name = "altText")]
    [Required]
    public string? AltText { get; init; }

    [FromForm(Name = "caption")]
    public string? Caption { get; init; }

    [FromForm(Name = "attribution")]
    public string? Attribution { get; init; }

    [FromForm(Name = "copyright")]
    public string? Copyright { get; init; }
}
