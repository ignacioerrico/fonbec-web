window.fonbecTheme = {
    set: function (value) {
        if (value !== "light" && value !== "dark") {
            return;
        }

        var maxAgeSeconds = 60 * 60 * 24 * 365;
        var secure = window.location.protocol === "https:" ? "; Secure" : "";
        document.cookie = "Fonbec.Theme=" + value
            + "; Path=/; Max-Age=" + maxAgeSeconds
            + "; SameSite=Lax" + secure;
    }
};