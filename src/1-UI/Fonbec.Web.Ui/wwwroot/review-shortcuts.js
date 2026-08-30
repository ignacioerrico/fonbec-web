// Document-level keyboard shortcuts for the reviewer workspace. Registered by the review panel
// while it is on screen and unregistered when it goes away.
window.fonbecReviewShortcuts = (function () {
    let handler = null;

    function isTypingTarget(target) {
        if (!target) {
            return false;
        }

        const tagName = target.tagName;
        return tagName === "INPUT"
            || tagName === "TEXTAREA"
            || tagName === "SELECT"
            || target.isContentEditable === true;
    }

    function isDialogOpen() {
        return document.querySelector(".mud-dialog-container .mud-dialog") !== null;
    }

    function resolveShortcut(e) {
        if (e.altKey || e.metaKey || e.repeat) {
            return null;
        }

        if (e.ctrlKey) {
            return e.key === "Enter" ? "Ctrl+Enter" : null;
        }

        if (e.key >= "1" && e.key <= "5") {
            return e.key;
        }

        if (e.key === "ArrowUp" || e.key === "ArrowDown") {
            return e.key;
        }

        return null;
    }

    return {
        register: function (dotNetRef) {
            window.fonbecReviewShortcuts.unregister();

            handler = function (e) {
                if (isTypingTarget(e.target) || isDialogOpen()) {
                    return;
                }

                const shortcut = resolveShortcut(e);
                if (shortcut === null) {
                    return;
                }

                e.preventDefault();
                dotNetRef.invokeMethodAsync("OnShortcutAsync", shortcut);
            };

            document.addEventListener("keydown", handler);
            return true;
        },

        unregister: function () {
            if (handler !== null) {
                document.removeEventListener("keydown", handler);
                handler = null;
            }

            return true;
        }
    };
})();