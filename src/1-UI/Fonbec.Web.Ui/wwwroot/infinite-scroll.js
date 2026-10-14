// Loads the next page when a sentinel scrolls into view.
window.fonbecInfiniteScroll = {
    observe: function (element, dotNetHelper) {
        this.disconnect(element);
        if (!element || !dotNetHelper) {
            return;
        }

        const observer = new IntersectionObserver(function (entries) {
            if (entries.some(function (entry) { return entry.isIntersecting; })) {
                dotNetHelper.invokeMethodAsync("LoadMoreDeliveredAsync");
            }
        }, { root: null, rootMargin: "240px" });

        observer.observe(element);
        element._fonbecScrollObserver = observer;
    },

    disconnect: function (element) {
        if (!element || !element._fonbecScrollObserver) {
            return;
        }

        element._fonbecScrollObserver.disconnect();
        delete element._fonbecScrollObserver;
    }
};