window.fonbecDownloadFile = function (fileName, contentType, base64) {
    const link = document.createElement("a");
    link.href = "data:" + contentType + ";base64," + base64;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
};