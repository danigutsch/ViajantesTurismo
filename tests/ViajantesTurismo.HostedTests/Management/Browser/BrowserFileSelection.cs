using System.Text;
using Microsoft.Playwright;
using SharedKernel.Testing.Playwright;

namespace ViajantesTurismo.HostedTests.Management.Browser;

/// <summary>Selects files in a Blazor <c>InputFile</c> once its client script is wired.</summary>
internal static class BrowserFileSelection
{
    /// <summary>Selects one in-memory file in the given file input.</summary>
    public static async Task Select(IPage page, string inputSelector, string fileName, string mimeType, string content)
    {
        await Select(page, inputSelector, [new FilePayload
        {
            Name = fileName,
            MimeType = mimeType,
            Buffer = Encoding.UTF8.GetBytes(content)
        }]);
    }

    /// <summary>Selects a batch of in-memory files in the given file input.</summary>
    public static async Task Select(IPage page, string inputSelector, IReadOnlyCollection<FilePayload> files)
    {
        var input = page.Locator(inputSelector);
        await input.SetBlazorInputFiles(files);
    }

    /// <summary>Dispatches a browser file drag event and reports whether its default action was canceled.</summary>
    public static async Task<bool> DispatchFileDrag(
        IPage page,
        string targetSelector,
        string eventName,
        IReadOnlyList<(string Name, string MediaType, int Length)> files)
    {
        return await page.Locator(targetSelector).EvaluateAsync<bool>(
            """
            (target, { eventName, files }) => {
                const dataTransfer = new DataTransfer();
                for (const { name, mediaType, length } of files) {
                    dataTransfer.items.add(new File([new Uint8Array(length)], name, { type: mediaType }));
                }
                return !target.dispatchEvent(new DragEvent(eventName, { dataTransfer, bubbles: true, cancelable: true }));
            }
            """,
            new
            {
                eventName,
                files = files.Select(file => new { name = file.Name, mediaType = file.MediaType, length = file.Length }).ToArray()
            });
    }
}
