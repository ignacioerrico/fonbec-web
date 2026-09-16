window.fonbecImageNaturalSize = function (img) {
    if (!img || !img.naturalWidth) {
        return null;
    }

    return { width: img.naturalWidth, height: img.naturalHeight };
};