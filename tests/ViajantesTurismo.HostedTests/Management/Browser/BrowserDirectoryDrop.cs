using Microsoft.Playwright;

namespace ViajantesTurismo.HostedTests.Management.Browser;

/// <summary>Dispatches deterministic directory reader pages through a real browser drop event.</summary>
internal static class BrowserDirectoryDrop
{
    /// <summary>Drops one directory that produces the supplied pages, followed by an empty page.</summary>
    public static Task Dispatch(
        ILocator target,
        IReadOnlyList<IReadOnlyList<string>> pages,
        int fileSize = 4,
        bool deferFirstPage = false) =>
        target.EvaluateAsync(
            """
            (zone, { pages, fileSize, deferFirstPage }) => {
                let readCount = 0;
                const folderName = "catalog-test-drop";
                const makeFileEntry = name => ({
                    isFile: true,
                    isDirectory: false,
                    name,
                    fullPath: `/${folderName}/${name}`,
                    file: onRead => queueMicrotask(() => onRead(new File([new Uint8Array(fileSize)], name, { type: "image/png" })))
                });
                const root = {
                    isFile: false,
                    isDirectory: true,
                    name: folderName,
                    fullPath: `/${folderName}`,
                    createReader: () => ({
                        readEntries: onRead => {
                            const page = pages[readCount++] ?? [];
                            zone.dataset.testDirectoryReads = readCount;
                            const reportPage = () => onRead(page.map(makeFileEntry));
                            if (deferFirstPage && readCount === 1) {
                                zone.addEventListener("test-directory-page-release", reportPage, { once: true });
                            } else {
                                queueMicrotask(reportPage);
                            }
                        }
                    })
                };
                const transfer = new DataTransfer();
                transfer.items.add(new File([], folderName));
                Object.defineProperty(transfer, "items", {
                    value: [{ kind: "file", webkitGetAsEntry: () => root }]
                });
                zone.dispatchEvent(new DragEvent("drop", { bubbles: true, cancelable: true, dataTransfer: transfer }));
            }
            """,
            new
            {
                pages,
                fileSize,
                deferFirstPage
            });
}
