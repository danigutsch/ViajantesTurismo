using Microsoft.AspNetCore.Components.Forms;

namespace ViajantesTurismo.Management.Web.Components.Pages.Catalog;

internal static class CatalogImageUploadFileReader
{
    public static Stream Open(IBrowserFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        return file.OpenReadStream(ViajantesTurismo.Catalog.Contracts.Application.ContractConstants.MaxMediaUploadBytes);
    }
}
