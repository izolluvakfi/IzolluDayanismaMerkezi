// Select2 Helper for Blazor
window.select2Helper = {
    initializeGnoSelect: function (elementId) {
        // Wait for jQuery and Select2 to be loaded
        const checkAndInit = () => {
            if (typeof $ !== 'undefined' && $.fn.select2) {
                try {
                    const $element = $('#' + elementId);
                    if ($element.length > 0) {
                        // Destroy existing select2 if any
                        if ($element.data('select2')) {
                            $element.select2('destroy');
                        }
                        // Initialize Select2
                        $element.select2({
                            placeholder: 'GNO Seçiniz',
                            allowClear: true,
                            width: '100%'
                        });
                        return true;
                    }
                } catch (error) {
                    console.error('Select2 initialization error:', error);
                }
            }
            return false;
        };

        // Try immediately
        if (checkAndInit()) {
            return;
        }

        // If not loaded, wait and retry
        let attempts = 0;
        const maxAttempts = 20;
        const interval = setInterval(() => {
            attempts++;
            if (checkAndInit() || attempts >= maxAttempts) {
                clearInterval(interval);
            }
        }, 100);
    },

    getValue: function (elementId) {
        try {
            const element = document.getElementById(elementId);
            return element ? element.value : '';
        } catch (error) {
            console.error('Error getting value:', error);
            return '';
        }
    },

    destroy: function (elementId) {
        if (typeof $ !== 'undefined' && $.fn.select2) {
            try {
                const $element = $('#' + elementId);
                if ($element.data('select2')) {
                    $element.select2('destroy');
                }
            } catch (error) {
                console.error('Error destroying select2:', error);
            }
        }
    }
};
