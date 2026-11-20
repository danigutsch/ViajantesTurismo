const zoneSelector = "[data-catalog-image-drop-zone]";

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

document.addEventListener("dragover", event => {
    const zone = getFileDropZone(event);
    if (!zone) {
        return;
    }

    event.preventDefault();
    const input = zone.querySelector("#imageFile");
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
    const input = zone.querySelector("#imageFile");
    if (zone.contains(event.target) && !input.disabled && event.dataTransfer.files.length > 0) {
        input.files = event.dataTransfer.files;
        input.dispatchEvent(new Event("change", { bubbles: true }));
    }
});
