using System.Text;
using Microsoft.Playwright;

namespace ViajantesTurismo.HostedTests.Management.Browser;

/// <summary>Selects files in a Blazor <c>InputFile</c> once its client script is wired.</summary>
internal static class BrowserFileSelection
{
    /// <summary>Selects one in-memory file in the given file input.</summary>
    public static async Task Select(IPage page, string inputSelector, string fileName, string mimeType, string content)
    {
        var input = page.Locator(inputSelector);
        await input.EvaluateAsync(
            "element => new Promise(resolve => { const check = () => element._blazorInputFileNextFileId !== undefined ? resolve() : setTimeout(check, 20); check(); })");
        await input.SetInputFilesAsync(new FilePayload
        {
            Name = fileName,
            MimeType = mimeType,
            Buffer = Encoding.UTF8.GetBytes(content)
        });
    }
}
