// Sidebar toggle - body class'ını değiştirir, CSS gerisini halleder
window.izolluToggleSidebar = function () {
    document.body.classList.toggle('sidebar-open');
};

window.izolluCloseSidebar = function () {
    document.body.classList.remove('sidebar-open');
};

// Sayfa navigasyonunda (Blazor enhanced navigation veya manuel) sidebar'ı kapat
document.addEventListener('click', function (e) {
    // NavMenu içindeki linklere tıklanırsa sidebar'ı kapat
    var target = e.target;
    while (target && target !== document) {
        if (target.tagName === 'A' && target.closest('.izollu-sidebar')) {
            // Küçük delay - Blazor navigation tamamlansın
            setTimeout(function () {
                document.body.classList.remove('sidebar-open');
            }, 100);
            break;
        }
        target = target.parentNode;
    }
});

// Ekran masaüstüne büyürse sidebar-open class'ını temizle
window.addEventListener('resize', function () {
    if (window.innerWidth > 960) {
        document.body.classList.remove('sidebar-open');
    }
});

// ESC tuşu ile sidebar kapat
document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') {
        document.body.classList.remove('sidebar-open');
    }
});
