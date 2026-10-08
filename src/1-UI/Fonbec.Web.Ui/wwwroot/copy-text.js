window.fonbecCopyText = async function (text) {
    if (!navigator.clipboard || !navigator.clipboard.writeText) {
        return false;
    }

    try {
        await navigator.clipboard.writeText(text ?? "");
        return true;
    } catch {
        return false;
    }
};