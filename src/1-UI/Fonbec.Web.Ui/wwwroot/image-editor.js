// Filerobot Image Editor 4.8.1 (MIT).
// The browser bundle is not published to npm, so wwwroot/lib/filerobot-image-editor/
// filerobot-image-editor.min.js is the official build:
// https://scaleflex.cloudimg.io/v7/plugins/filerobot-image-editor/4.8.1/filerobot-image-editor.min.js
window.fonbecImageEditor = (function () {
    const pending = new Map();
    const sessions = new Map();
    let scriptPromise = null;

    const translations = {
        name: "Nombre",
        save: "Guardar",
        saveAs: "Guardar como",
        back: "Volver",
        loading: "Cargando...",
        resetOperations: "Deshacer todos los cambios",
        changesLoseWarningHint: "Si confirmás, se pierden los cambios. ¿Seguís?",
        discardChangesWarningHint: "Si cerrás, el último cambio no se guarda.",
        cancel: "Cancelar",
        apply: "Aplicar",
        warning: "Aviso",
        confirm: "Confirmar",
        discardChanges: "Descartar cambios",
        undoTitle: "Deshacer",
        redoTitle: "Rehacer",
        showImageTitle: "Ver la imagen original",
        zoomInTitle: "Acercar",
        zoomOutTitle: "Alejar",
        toggleZoomMenuTitle: "Zoom",
        adjustTab: "Ajustar",
        finetuneTab: "Afinar",
        filtersTab: "Filtros",
        watermarkTab: "Marca de agua",
        annotateTabLabel: "Anotar",
        resize: "Tamaño",
        resizeTab: "Tamaño",
        imageName: "Nombre de la imagen",
        invalidImageError: "La imagen no es válida.",
        uploadImageError: "No se pudo cargar la imagen.",
        areNotImages: "no son imágenes",
        isNotImage: "no es una imagen",
        toBeUploaded: "para subir",
        cropTool: "Recortar",
        original: "Original",
        custom: "Libre",
        square: "Cuadrado",
        landscape: "Horizontal",
        portrait: "Vertical",
        ellipse: "Elipse",
        classicTv: "TV clásica",
        cinemascope: "Cinemascope",
        arrowTool: "Flecha",
        blurTool: "Desenfoque",
        brightnessTool: "Brillo",
        contrastTool: "Contraste",
        ellipseTool: "Elipse",
        unFlipX: "Deshacer volteo horizontal",
        flipX: "Voltear horizontal",
        unFlipY: "Deshacer volteo vertical",
        flipY: "Voltear vertical",
        hsvTool: "HSV",
        hue: "Tono",
        brightness: "Brillo",
        saturation: "Saturación",
        value: "Valor",
        imageTool: "Imagen",
        importing: "Importando...",
        addImage: "+ Agregar imagen",
        uploadImage: "Subir imagen",
        fromGallery: "De la galería",
        lineTool: "Línea",
        penTool: "Lápiz",
        polygonTool: "Polígono",
        sides: "Lados",
        rectangleTool: "Rectángulo",
        cornerRadius: "Radio de las esquinas",
        resizeWidthTitle: "Ancho en píxeles",
        resizeHeightTitle: "Alto en píxeles",
        toggleRatioLockTitle: "Mantener la proporción",
        resetSize: "Volver al tamaño original",
        rotateTool: "Rotar",
        textTool: "Texto",
        textSpacings: "Espaciado del texto",
        textAlignment: "Alineación del texto",
        fontFamily: "Fuente",
        size: "Tamaño",
        letterSpacing: "Espaciado entre letras",
        lineHeight: "Interlineado",
        warmthTool: "Calidez",
        addWatermark: "+ Agregar marca de agua",
        addTextWatermark: "+ Agregar marca de agua de texto",
        addWatermarkTitle: "Elegí el tipo de marca de agua",
        uploadWatermark: "Subir marca de agua",
        addWatermarkAsText: "Agregar como texto",
        padding: "Relleno",
        paddings: "Rellenos",
        shadow: "Sombra",
        horizontal: "Horizontal",
        vertical: "Vertical",
        blur: "Desenfoque",
        opacity: "Opacidad",
        transparency: "Transparencia",
        position: "Posición",
        stroke: "Trazo",
        saveAsModalTitle: "Guardar como",
        extension: "Extensión",
        format: "Formato",
        nameIsRequired: "El nombre es obligatorio.",
        quality: "Calidad",
        imageDimensionsHoverTitle: "Tamaño guardado (ancho x alto)",
        cropSizeLowerThanResizedWarning:
            "El recorte es más chico que el tamaño aplicado y puede perder nitidez.",
        actualSize: "Tamaño real (100%)",
        fitSize: "Ajustar a la pantalla",
        addImageTitle: "Elegí una imagen para agregar...",
        mutualizedFailedToLoadImg: "No se pudo cargar la imagen.",
        tabsMenu: "Menú",
        download: "Descargar",
        width: "Ancho",
        height: "Alto",
        plus: "+",
        cropItemNoEffect: "Esta opción de recorte no tiene vista previa",
    };

    function key(documentId, pageIndex) {
        return String(documentId) + ":" + String(pageIndex);
    }

    function loadScript(scriptUrl) {
        if (window.FilerobotImageEditor) {
            return Promise.resolve();
        }

        if (scriptPromise) {
            return scriptPromise;
        }

        scriptPromise = new Promise(function (resolve, reject) {
            const script = document.createElement("script");
            // A relative src would follow the page URL, not <base href>.
            script.src = new URL(scriptUrl, document.baseURI).href;
            script.onload = function () {
                if (window.FilerobotImageEditor) {
                    resolve();
                } else {
                    scriptPromise = null;
                    reject(new Error("No se pudo cargar el editor de imágenes."));
                }
            };
            script.onerror = function () {
                scriptPromise = null;
                reject(new Error("No se pudo cargar el editor de imágenes."));
            };
            document.head.appendChild(script);
        });

        return scriptPromise;
    }

    function finish(session, result) {
        if (!session || session.settled) {
            return;
        }

        session.settled = true;
        sessions.delete(session.id);
        const editor = session.editor;
        session.editor = null;
        session.resolve(result);
        // Unmount after Filerobot finishes the click that asked to close. Terminating
        // inside that click throws once the image has been edited.
        if (editor) {
            setTimeout(function () {
                try {
                    editor.terminate();
                } catch (e) {
                    // The editor may already be unmounted.
                }
            }, 0);
        }
    }

    function bindImmediateClose(container, session) {
        container.addEventListener("click", function (event) {
            if (session.settled || !event.target || !event.target.closest) {
                return;
            }

            if (!event.target.closest(".FIE_topbar-close-button")) {
                return;
            }

            // After an edit, Filerobot's own handler opens a discard prompt and does not close.
            event.preventDefault();
            event.stopPropagation();
            event.stopImmediatePropagation();
            finish(session, { saved: false });
        }, true);
    }

    function blobFromCanvas(canvas, quality) {
        return new Promise(function (resolve, reject) {
            canvas.toBlob(function (blob) {
                if (blob) {
                    resolve(blob);
                } else {
                    reject(new Error("No se pudo exportar la imagen."));
                }
            }, "image/jpeg", quality);
        });
    }

    function dataUrlToBlob(dataUrl) {
        const comma = dataUrl.indexOf(",");
        const header = dataUrl.slice(0, comma);
        const body = dataUrl.slice(comma + 1);
        const mimeMatch = /^data:([^;]+)/.exec(header);
        const mime = mimeMatch ? mimeMatch[1] : "image/jpeg";
        const binary = atob(body);
        const bytes = new Uint8Array(binary.length);
        for (let i = 0; i < binary.length; i++) {
            bytes[i] = binary.charCodeAt(i);
        }

        return new Blob([bytes], { type: mime });
    }

    function loadImage(src) {
        return new Promise(function (resolve, reject) {
            const image = new Image();
            image.onload = function () {
                resolve(image);
            };
            image.onerror = function () {
                reject(new Error("No se pudo exportar la imagen."));
            };
            image.src = src;
        });
    }

    async function forceJpegBlob(imageData) {
        const quality = typeof imageData.quality === "number" ? imageData.quality : 0.92;
        if (imageData.imageCanvas && typeof imageData.imageCanvas.toBlob === "function") {
            try {
                return await blobFromCanvas(imageData.imageCanvas, quality);
            } catch (e) {
                // Fall through to the base64 export.
            }
        }

        if (typeof imageData.imageBase64 !== "string" || imageData.imageBase64.length === 0) {
            throw new Error("No se pudo exportar la imagen.");
        }

        if (imageData.imageBase64.startsWith("data:image/jpeg")
            || imageData.imageBase64.startsWith("data:image/jpg")) {
            return dataUrlToBlob(imageData.imageBase64);
        }

        const image = await loadImage(imageData.imageBase64);
        const canvas = document.createElement("canvas");
        canvas.width = image.naturalWidth || image.width;
        canvas.height = image.naturalHeight || image.height;
        const context = canvas.getContext("2d");
        if (!context) {
            throw new Error("No se pudo exportar la imagen.");
        }

        context.drawImage(image, 0, 0);
        return blobFromCanvas(canvas, quality);
    }

    function store(documentId, pageIndex, bytes) {
        const pendingKey = key(documentId, pageIndex);
        const previous = pending.get(pendingKey);
        if (previous) {
            URL.revokeObjectURL(previous.url);
        }

        const url = URL.createObjectURL(new Blob([bytes], { type: "image/jpeg" }));
        pending.set(pendingKey, { bytes: bytes, url: url });
    }

    function remember(documentId, pageIndex, sourceUrl) {
        const entry = pending.get(key(documentId, pageIndex));
        return entry ? entry.url : sourceUrl;
    }

    return {
        open: function (container, options) {
            return loadScript(options.scriptUrl).then(function () {
                return new Promise(function (resolve) {
                    const session = {
                        id: options.sessionId,
                        resolve: resolve,
                        settled: false,
                        editor: null,
                    };
                    sessions.set(session.id, session);

                    const fileName = options.fileName;
                    const baseName = fileName.replace(/\.jpe?g$/i, "");
                    const config = {
                        source: remember(options.documentId, options.pageIndex, options.sourceUrl),
                        // Same-origin pages and blob URLs must keep cookies and stay untainted.
                        noCrossOrigin: true,
                        useBackendTranslations: false,
                        language: "es",
                        translations: translations,
                        avoidChangesNotSavedAlertOnLeave: true,
                        tabsIds: ["Adjust", "Finetune", "Resize"],
                        defaultTabId: "Adjust",
                        defaultToolId: "Crop",
                        Crop: {
                            noPresets: true,
                            ratio: "custom",
                        },
                        Rotate: {
                            angle: 90,
                            componentType: "slider",
                        },
                        defaultSavedImageType: "jpeg",
                        defaultSavedImageName: baseName,
                        defaultSavedImageQuality: 0.92,
                        // Caps at the source resolution so a fitted preview is not what gets saved.
                        savingPixelRatio: 4,
                        disableSaveIfNoChanges: true,
                        // Returning false skips the save dialog. onSave then receives the JPEG data.
                        onBeforeSave: function () {
                            return false;
                        },
                        onSave: function (imageData) {
                            const data = imageData && imageData.imageData ? imageData.imageData : imageData;
                            return forceJpegBlob(data).then(function (blob) {
                                return blob.arrayBuffer();
                            }).then(function (buffer) {
                                const bytes = new Uint8Array(buffer);
                                store(options.documentId, options.pageIndex, bytes);
                                finish(session, {
                                    saved: true,
                                    size: bytes.byteLength,
                                    name: fileName,
                                });
                            }).catch(function () {
                                finish(session, {
                                    saved: false,
                                    error: "No se pudo guardar la imagen editada.",
                                });
                            });
                        },
                        onClose: function () {
                            finish(session, { saved: false });
                        },
                    };

                    const editor = new window.FilerobotImageEditor(container, config);
                    session.editor = editor;
                    bindImmediateClose(container, session);
                    editor.render();
                });
            });
        },

        cancel: function (sessionId) {
            finish(sessions.get(sessionId), { saved: false });
        },

        clear: function (documentId, pageIndex) {
            const pendingKey = key(documentId, pageIndex);
            const entry = pending.get(pendingKey);
            if (!entry) {
                return;
            }

            URL.revokeObjectURL(entry.url);
            pending.delete(pendingKey);
        },

        clearDocument: function (documentId) {
            const prefix = String(documentId) + ":";
            for (const pendingKey of Array.from(pending.keys())) {
                if (!pendingKey.startsWith(prefix)) {
                    continue;
                }

                URL.revokeObjectURL(pending.get(pendingKey).url);
                pending.delete(pendingKey);
            }
        },

        take: function (documentId, pageIndex) {
            const entry = pending.get(key(documentId, pageIndex));
            if (!entry) {
                throw new Error("La imagen editada ya no está disponible. Volvé a editarla.");
            }

            return entry.bytes.slice();
        },
    };
})();