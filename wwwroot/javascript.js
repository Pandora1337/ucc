
window.clickElement = (elementId) => {
    document.getElementById(elementId).click();
};

window.focusElement = (elementId) => {
    document.getElementById(elementId).focus();
};

window.downloadFileBytes = async (filename, fileBytes) => {
    const url = createBlobUrl(fileBytes, { type: "application/json"});

    const a = document.createElement("a");
    a.href = url;
    a.download = filename;
    a.click();
    a.remove();

    URL.revokeObjectURL(url);
};

window.createBlobUrl = (fileBytes, fileType) => {
    const blob = new Blob([fileBytes], { type: fileType });
    return URL.createObjectURL(blob);
};

window.previewImage = (inputElement, imgElement) => {
    const url = URL.createObjectURL(inputElement.files[0]);
    imgElement.addEventListener('load', () => URL.revokeObjectURL(url), { once: true });
    imgElement.addEventListener('error', () => URL.revokeObjectURL(url), { once: true });
    imgElement.src = url;
};
