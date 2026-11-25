using Microsoft.AspNetCore.Components.Forms;
using SharedKernel.HttpClients;

namespace ViajantesTurismo.Management.Web.Components.Pages.Catalog;

/// <summary>Retains one selected browser file and its upload state until the picker changes.</summary>
internal sealed class CatalogImageUploadEntry(IBrowserFile file, int index, string? fileError)
{
    /// <summary>The browser file handle for the current selection.</summary>
    public IBrowserFile File { get; } = file;

    /// <summary>The stable identifier of the description field.</summary>
    public string DescriptionId => index == 0 ? "altText" : $"altText-{index}";

    /// <summary>The stable identifier of the caption field.</summary>
    public string CaptionId => index == 0 ? "caption" : $"caption-{index}";

    /// <summary>The description to send for this image.</summary>
    public string AltText { get; set; } = string.Empty;

    /// <summary>The optional caption to send for this image.</summary>
    public string? Caption
    {
        get; set;
    }

    /// <summary>The client guidance shown when the selected file cannot be sent.</summary>
    public string? FileError { get; } = fileError;

    /// <summary>The current validation or upload error.</summary>
    public string? Error
    {
        get; set;
    }

    /// <summary>The transport and server-processing state of this image.</summary>
    public UploadOperationState Operation { get; } = new();

    /// <summary>Whether this image can start its first upload.</summary>
    public bool IsQueued => FileError is null && Operation.Status == UploadOperationStatus.None;
}
