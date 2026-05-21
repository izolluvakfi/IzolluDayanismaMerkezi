window.isMobileScreen = function () {
    return window.innerWidth < 960;
};

window.registerResizeCallback = function (dotnetRef) {
    let resizeTimer;
    window.addEventListener('resize', function () {
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(function () {
            dotnetRef.invokeMethodAsync('OnScreenResize', window.innerWidth < 960);
        }, 150);
    });
};
