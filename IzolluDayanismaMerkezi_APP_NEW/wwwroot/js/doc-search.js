// Kullanım Kılavuzu arama ve vurgulama fonksiyonu
window.highlightSearchText = function (searchText) {
    const contentDiv = document.getElementById('user-guide-content');
    if (!contentDiv) return;

    // Önceki vurguları temizle
    removeHighlights(contentDiv);

    if (!searchText || searchText.trim() === '') {
        return;
    }

    const searchTerm = searchText.trim().toLowerCase();
    const walker = document.createTreeWalker(
        contentDiv,
        NodeFilter.SHOW_TEXT,
        null,
        false
    );

    const nodesToHighlight = [];
    let node;

    while (node = walker.nextNode()) {
        const text = node.nodeValue;
        const lowerText = text.toLowerCase();
        
        if (lowerText.includes(searchTerm)) {
            nodesToHighlight.push({
                node: node,
                text: text
            });
        }
    }

    // Vurgulama işlemini uygula
    nodesToHighlight.forEach(item => {
        const regex = new RegExp(`(${escapeRegex(searchTerm)})`, 'gi');
        const parts = item.text.split(regex);
        
        if (parts.length > 1) {
            const fragment = document.createDocumentFragment();
            
            parts.forEach(part => {
                if (part.toLowerCase() === searchTerm) {
                    const mark = document.createElement('mark');
                    mark.className = 'search-highlight';
                    mark.textContent = part;
                    fragment.appendChild(mark);
                } else {
                    fragment.appendChild(document.createTextNode(part));
                }
            });
            
            item.node.parentNode.replaceChild(fragment, item.node);
        }
    });

    // İlk eşleşmeye scroll yap
    const firstHighlight = contentDiv.querySelector('.search-highlight');
    if (firstHighlight) {
        firstHighlight.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
};

// Vurguları temizleme fonksiyonu
function removeHighlights(container) {
    const highlights = container.querySelectorAll('.search-highlight');
    highlights.forEach(highlight => {
        const parent = highlight.parentNode;
        parent.replaceChild(document.createTextNode(highlight.textContent), highlight);
        parent.normalize();
    });
}

// Regex escape fonksiyonu
function escapeRegex(string) {
    return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
