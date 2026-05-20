window.bootstrapSelect = window.bootstrapSelect || {};

(function () {
    function ensurePlugin() {
        return window.jQuery && window.jQuery.fn && window.jQuery.fn.selectpicker;
    }

    function waitForElement(selector, callback, maxAttempts) {
        maxAttempts = maxAttempts || 20;
        var attempts = 0;

        function check() {
            var element = document.querySelector(selector);
            if (element) {
                callback(element);
            } else if (attempts < maxAttempts) {
                attempts++;
                setTimeout(check, 100);
            } else {
                console.warn("Element not found after max attempts:", selector);
            }
        }
        check();
    }

    // Initialize all selectpickers on the page
    window.bootstrapSelect.init = function () {
        if (!ensurePlugin()) {
            console.warn("bootstrap-select plug-in is not available.");
            return;
        }

        window.setTimeout(function () {
            window.jQuery(".selectpicker").each(function () {
                var $this = window.jQuery(this);
                // Destroy if already initialized, then reinitialize
                if ($this.data('selectpicker')) {
                    $this.selectpicker('destroy');
                }
                $this.selectpicker({
                    liveSearch: true,
                    size: 10,
                    container: 'body',
                    dropupAuto: false
                });
            });
        }, 50);
    };

    // Initialize a specific selectpicker by ID
    window.bootstrapSelect.initById = function (elementId) {
        if (!ensurePlugin()) {
            console.warn("bootstrap-select plug-in is not available.");
            return;
        }

        waitForElement('#' + elementId, function (element) {
            var $el = window.jQuery(element);

            // Destroy existing instance if any
            if ($el.data('selectpicker')) {
                $el.selectpicker('destroy');
            }

            // Initialize with options
            $el.selectpicker({
                liveSearch: true,
                size: 10,
                container: 'body',
                dropupAuto: false
            });

            console.log("bootstrap-select initialized for:", elementId);
        });
    };

    // Refresh all selectpickers
    window.bootstrapSelect.refresh = function () {
        if (!ensurePlugin()) {
            return;
        }

        window.setTimeout(function () {
            window.jQuery(".selectpicker").selectpicker("refresh");
        }, 50);
    };

    // Refresh a specific selectpicker by ID - destroy and reinitialize to sync with Blazor DOM
    window.bootstrapSelect.refreshById = function (elementId) {
        if (!ensurePlugin()) {
            return;
        }

        var $el = window.jQuery('#' + elementId);
        if ($el.length) {
            // Destroy existing instance
            if ($el.data('selectpicker')) {
                $el.selectpicker('destroy');
            }
            // Reinitialize to read fresh DOM state
            $el.selectpicker({
                liveSearch: true,
                size: 10,
                container: 'body',
                dropupAuto: false
            });
            console.log("bootstrap-select refreshed (destroy+init) for:", elementId);
        }
    };

    // Destroy all selectpickers
    window.bootstrapSelect.destroy = function () {
        if (!ensurePlugin()) {
            return;
        }

        window.jQuery(".selectpicker").each(function () {
            var $this = window.jQuery(this);
            if ($this.data('selectpicker')) {
                $this.selectpicker('destroy');
            }
        });
    };

    // Destroy a specific selectpicker by ID
    window.bootstrapSelect.destroyById = function (elementId) {
        if (!ensurePlugin()) {
            return;
        }

        var $el = window.jQuery('#' + elementId);
        if ($el.length && $el.data('selectpicker')) {
            $el.selectpicker('destroy');
        }
    };

    // Set value programmatically
    window.bootstrapSelect.setValue = function (elementId, value) {
        if (!ensurePlugin()) {
            return;
        }

        var $el = window.jQuery('#' + elementId);
        if ($el.length) {
            $el.val(value);
            $el.selectpicker('refresh');
        }
    };
})();

window.downloadFile = function (filename, base64Data) {
    const link = document.createElement('a');
    link.download = filename;
    link.href = "data:application/octet-stream;base64," + base64Data;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};
