using Microsoft.AspNetCore.Components.Forms;

namespace ViajantesTurismo.Management.Web.Components.Pages.Catalog;

/// <summary>Opens browser files selected for catalog image upload.</summary>
internal static class CatalogImageUploadFileReader
{
    /// <summary>Opens a read stream limited to the maximum media upload size.</summary>
    public static Stream Open(IBrowserFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        return file.OpenReadStream(ViajantesTurismo.Catalog.Contracts.Application.ContractConstants.MaxMediaUploadBytes);
    }
}
