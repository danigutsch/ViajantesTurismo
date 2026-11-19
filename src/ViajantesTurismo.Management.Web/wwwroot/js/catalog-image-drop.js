const zoneSelector = "[data-catalog-image-drop-zone]";
const fileInputSelector = "#imageFile";
const folderInputSelector = "#imageFolder";
const maximumVisitedEntries = 1024;
const folderSelections = new WeakMap();
const canAssembleFiles = typeof DataTransfer === "function";

document.documentElement.toggleAttribute("data-catalog-folder-input-supported",
    canAssembleFiles && "webkitdirectory" in document.createElement("input"));

class FolderSelectionError extends Error {
    constructor(messageKey) {
        super();
        this.messageKey = messageKey;
    }
}

function getFileDropZone(event) {
    if (event.dataTransfer?.types.includes("Files")) {
        return document.querySelector(zoneSelector);
    }
    return null;
}

function setDragActive(zone, active) {
    if (zone.hasAttribute("data-drag-active") === active) {
        return;
    }

    zone.toggleAttribute("data-drag-active", active);
    const feedback = zone.querySelector("[data-catalog-image-drop-feedback]");
    feedback.textContent = active ? feedback.dataset.releaseMessage : "";
}

function showFolderError(zone, messageKey) {
    const feedback = zone.querySelector("[data-catalog-image-folder-error]");
    feedback.setAttribute("role", messageKey ? "alert" : "none");
    feedback.hidden = !messageKey;
    feedback.textContent = messageKey ? zone.dataset[messageKey] : "";
}

function isCurrent(selection) {
    const { zone, input } = selection;
    return input.isConnected && !input.disabled && folderSelections.get(zone) === selection;
}

function applyFiles(input, files) {
    input.files = files;
    input.dispatchEvent(new Event("change", { bubbles: true }));
}

function addFolderFile(selection, file) {
    if (selection.files.length >= selection.maximumFiles) {
        throw new FolderSelectionError("fileCountMessage");
    }
    if (file.size > selection.maximumFileBytes) {
        throw new FolderSelectionError("fileSizeMessage");
    }
    if (file.size > selection.maximumTotalBytes - selection.totalBytes) {
        throw new FolderSelectionError("totalSizeMessage");
    }

    selection.files.push(file);
    selection.totalBytes += file.size;
}

async function queueFolder(zone, input, collectFiles) {
    const selection = {
        zone,
        input,
        files: [],
        totalBytes: 0,
        visitedEntries: 0,
        maximumFiles: Number(zone.dataset.maximumFiles),
        maximumFileBytes: Number(zone.dataset.maximumFileBytes),
        maximumTotalBytes: Number(zone.dataset.maximumTotalBytes)
    };
    folderSelections.set(zone, selection);
    showFolderError(zone);
    try {
        await collectFiles(selection);
        if (isCurrent(selection) && selection.files.length > 0) {
            const transfer = new DataTransfer();
            for (const file of selection.files) {
                transfer.items.add(file);
            }
            applyFiles(input, transfer.files);
        }
    } catch (error) {
        if (isCurrent(selection)) {
            showFolderError(zone, error instanceof FolderSelectionError ? error.messageKey : "folderReadMessage");
        }
    } finally {
        if (folderSelections.get(zone) === selection) {
            folderSelections.delete(zone);
        }
    }
}

function captureFolderDrop(transfer) {
    const entries = [];
    for (const item of transfer.items ?? []) {
        if (item.kind === "file") {
            const getEntry = item.getAsEntry ?? item.webkitGetAsEntry;
            const entry = getEntry?.call(item) ?? item.getAsFile();
            if (entry) {
                entries.push(entry);
            }
        }
    }

    return entries.some(entry => entry.isDirectory) ? entries : null;
}

async function collectEntry(selection, entry) {
    if (!isCurrent(selection)) {
        return;
    }
    if (++selection.visitedEntries > maximumVisitedEntries) {
        throw new FolderSelectionError("traversalLimitMessage");
    }

    if (entry instanceof File) {
        addFolderFile(selection, entry);
    } else if (entry.isFile) {
        const file = await new Promise((resolve, reject) => entry.file(resolve, reject));
        if (isCurrent(selection)) {
            addFolderFile(selection, file);
        }
    } else if (entry.isDirectory) {
        const reader = entry.createReader();
        while (isCurrent(selection)) {
            const page = await new Promise((resolve, reject) => reader.readEntries(resolve, reject));
            if (page.length === 0) {
                return;
            }
            for (const child of page) {
                await collectEntry(selection, child);
            }
        }
    }
}

document.addEventListener("change", event => {
    const picker = event.target;
    if (!(picker instanceof HTMLInputElement) || !picker.matches(`${fileInputSelector}, ${folderInputSelector}`)) {
        return;
    }
    const zone = picker.closest(zoneSelector);
    if (!zone) {
        return;
    }

    folderSelections.delete(zone);
    showFolderError(zone);
    if (picker.matches(folderInputSelector) && picker.files.length > 0) {
        const input = zone.querySelector(fileInputSelector);
        if (!input.disabled) {
            queueFolder(zone, input, selection => {
                if (picker.files.length > selection.maximumFiles) {
                    throw new FolderSelectionError("fileCountMessage");
                }
                for (const file of picker.files) {
                    addFolderFile(selection, file);
                }
            });
        }
        picker.value = "";
    }
}, { capture: true });

document.addEventListener("dragover", event => {
    const zone = getFileDropZone(event);
    if (!zone) {
        return;
    }

    event.preventDefault();
    const input = zone.querySelector(fileInputSelector);
    const active = zone.contains(event.target) && !input.disabled;
    event.dataTransfer.dropEffect = active ? "copy" : "none";
    setDragActive(zone, active);
});

document.addEventListener("dragleave", event => {
    const zone = getFileDropZone(event);
    if (zone && !(event.relatedTarget instanceof Node && zone.contains(event.relatedTarget))) {
        setDragActive(zone, false);
    }
});

document.addEventListener("drop", event => {
    const zone = getFileDropZone(event);
    if (!zone) {
        return;
    }

    event.preventDefault();
    setDragActive(zone, false);
    const input = zone.querySelector(fileInputSelector);
    if (!zone.contains(event.target) || input.disabled) {
        return;
    }

    const transfer = event.dataTransfer;
    folderSelections.delete(zone);
    showFolderError(zone);
    if (canAssembleFiles && transfer.items?.length > maximumVisitedEntries) {
        showFolderError(zone, "traversalLimitMessage");
        return;
    }
    const entries = canAssembleFiles ? captureFolderDrop(transfer) : null;
    if (entries) {
        queueFolder(zone, input, async selection => {
            for (const entry of entries) {
                await collectEntry(selection, entry);
            }
        });
    } else if (transfer.files.length > 0) {
        applyFiles(input, transfer.files);
    }
});
