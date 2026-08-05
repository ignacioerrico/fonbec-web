window.fonbecBurstConfetti = function (originElement) {
    if (typeof confetti !== "function") {
        return false;
    }

    setTimeout(function () {
        var origin = { x: 0.5, y: 0.6 };

        if (originElement) {
            var rect = originElement.getBoundingClientRect();
            origin = {
                x: (rect.left + rect.width / 2) / window.innerWidth,
                y: (rect.top + rect.height / 2) / window.innerHeight
            };
        }

        confetti({
            particleCount: 100,
            spread: 70,
            origin: origin,
            ticks: 120,
            decay: 0.9
        });
    }, 750);

    return true;
};