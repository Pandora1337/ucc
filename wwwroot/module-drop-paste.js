export function initializeFilePasteZone(pasteZoneElement, inputFile) {
    function onPaste(e) {
        const file = e.clipboardData?.files?.[0];
        if (!file)
            return;

        e.preventDefault();

        // for some reason firefox takes issue with a filelist,
        // hence this monstrocity
        const dt = new DataTransfer();
        dt.items.add(file);
        inputFile.files = dt.files;

        const event = new Event('change', { bubbles: true });
        inputFile.dispatchEvent(event);
    }

    // Register event
    pasteZoneElement.addEventListener('paste', onPaste);

    // The returned object allows to unregister the event when the Blazor component is destroyed
    return {
        dispose: () => {
            pasteZoneElement.removeEventListener('paste', onPaste);
        }
    }
}

export function initializeFileDropZone(dropZoneElement, inputFile) {
    // Add a class when the user drags a file over the drop zone
    function onDragHover(e) {
        e.preventDefault();
        dropZoneElement.classList.add("drop-hover");
    }

    function onDragLeave(e) {
        e.preventDefault();
        dropZoneElement.classList.remove("drop-hover");
    }

    // Handle the paste and drop events
    function onDrop(e) {
        onDragLeave(e);

        const files = e.dataTransfer?.files;
        if (!files || files.length === 0)
            return;

        // Set the files property of the input element and raise the change event
        inputFile.files = files
        const event = new Event('change', { bubbles: true });
        inputFile.dispatchEvent(event);
    }

    // Register all events
    dropZoneElement.addEventListener("dragenter", onDragHover);
    dropZoneElement.addEventListener("dragover", onDragHover);
    dropZoneElement.addEventListener("dragleave", onDragLeave);
    dropZoneElement.addEventListener("drop", onDrop);

    // The returned object allows to unregister the events when the Blazor component is destroyed
    return {
        dispose: () => {
            dropZoneElement.removeEventListener('dragenter', onDragHover);
            dropZoneElement.removeEventListener('dragover', onDragHover);
            dropZoneElement.removeEventListener('dragleave', onDragLeave);
            dropZoneElement.removeEventListener("drop", onDrop);
        }
    }
}