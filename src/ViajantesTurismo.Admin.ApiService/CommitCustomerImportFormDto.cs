using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.ApiService;

/// <summary>
/// Represents the multipart form fields used to commit a customer import with conflict resolutions.
/// </summary>
internal readonly record struct CommitCustomerImportFormDto
{
    /// <summary>
    /// Gets or sets the CSV file to import.
    /// </summary>
    [Required]
    [FromForm(Name = "file")]
    public required IFormFile File { get; init; }

    /// <summary>
    /// Gets or sets the serialized conflict-resolution payload.
    /// </summary>
    [FromForm(Name = "conflictResolutions")]
    public string? ConflictResolutions { get; init; }
}
